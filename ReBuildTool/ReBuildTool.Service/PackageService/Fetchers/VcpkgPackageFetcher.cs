using System.Runtime.InteropServices;
using NiceIO;
using Newtonsoft.Json;
using ReBuildTool.Service.Global;
using ResetCore.Common;

namespace ReBuildTool.Service.PackageService.Fetchers;

/// <summary>
/// Bridges a vcpkg port into rbt's package model.
///
/// vcpkg does the acquiring and building; this fetcher's job is translation. After
/// <c>vcpkg install</c> it writes an <c>RBTPackage.json</c> describing the installed tree as a
/// prebuilt binary package, so the ordinary binary-package path
/// (<c>PackageModuleBinder</c> / <c>PackageArtifactSelector</c>) takes it from there and nothing
/// downstream needs to know vcpkg exists.
///
/// The vcpkg checkout is shared across projects under <c>$RBT_HOME/vcpkg</c> rather than kept per
/// project: a populated vcpkg tree is large and slow to rebuild, and it does not belong to any one
/// project's working tree.
/// </summary>
public class VcpkgPackageFetcher : IPackageFetcher
{
	/// <summary>
	/// The vcpkg tool itself is pinned so that a restore is reproducible. Ports move independently;
	/// this is the tooling, not the library.
	/// </summary>
	private const string VcpkgToolTag = "2024.02.14";

	private const string VcpkgUrl = "https://github.com/microsoft/vcpkg.git";

	public PackageSourceKind Kind => PackageSourceKind.Vcpkg;

	public static NPath VcpkgRoot => GlobalPaths.ReBuildToolHome.Combine("vcpkg");

	public FetchedPackage Fetch(FetchRequest request)
	{
		var port = request.Dependency.Vcpkg!;
		var triplet = request.Dependency.EffectiveTriplet;
		var installed = VcpkgRoot.Combine("installed", triplet);
		var destination = request.DefaultDestination;

		var infoRoot = VcpkgRoot.Combine("installed", "vcpkg", "info");
		var alreadyInstalled = IsPortInstalled(installed, infoRoot, port, triplet);

		if (!alreadyInstalled || request.Options.Force)
		{
			if (request.Options.Offline)
			{
				throw new PackageException(
					$"--Offline was requested but vcpkg port \"{port}\" ({triplet}) is not installed yet. " +
					$"Run a restore without --Offline first.");
			}
			EnsureBootstrapped(request);
			Log.Info($"[package] vcpkg install {port}:{triplet}");
			ProcessRunner.RunOrThrow(
				VcpkgExecutable().ToString(),
				new[] { "install", $"{port}:{triplet}", "--recurse" },
				VcpkgRoot,
				$"installing vcpkg port \"{port}\"");
		}

		if (!installed.DirectoryExists())
		{
			throw new PackageException(
				$"vcpkg port \"{port}\" reported success but {installed} does not exist. " +
				$"Is \"{triplet}\" a triplet this vcpkg supports?");
		}

		// The synthesized package is a directory holding nothing but a manifest; the headers and
		// libraries stay where vcpkg put them and are referenced by absolute path.
		destination.EnsureDirectoryExists();
		var manifest = DescribeInstalledTree(request.Name, port, installed);
		WriteIfChanged(PackageManifest.PathIn(destination), manifest);

		return new FetchedPackage(destination, $"{port}:{triplet}");
	}

	/// <summary>
	/// Renders a vcpkg installed tree as a binary-package manifest.
	///
	/// Separated from the install so it can be exercised without a vcpkg checkout: the mapping (and
	/// vcpkg's debug/release split) is the part with decisions in it, the install is just a
	/// subprocess.
	/// </summary>
	public static string DescribeInstalledTree(string packageName, string port, NPath installed)
	{
		var manifest = new PackageManifest
		{
			Name = packageName,
			Binary = new PackageBinarySpec
			{
				Module = packageName,
				Includes = { installed.Combine("include").ToString() }
			}
		};

		// vcpkg keeps the debug build in a parallel debug/ prefix. Mapping it to rbt's Debug
		// configuration is the whole reason this is not a single artifact.
		var infoRoot = installed.Parent.Combine("vcpkg", "info");
		var ownedFiles = PortInfoFiles(infoRoot, port, installed.FileName)
			.SelectMany(file => file.ReadAllLines())
			.Select(path => path.Replace('\\', '/'))
			.ToList();
		var release = LibrariesIn(installed.Combine("lib"), ownedFiles, $"{installed.FileName}/lib/");
		var debug = LibrariesIn(
			installed.Combine("debug", "lib"),
			ownedFiles,
			$"{installed.FileName}/debug/lib/");

		if (debug.Count > 0)
		{
			manifest.Binary.Artifacts.Add(new PackageBinaryArtifact
			{
				Config = "Debug",
				LibraryDirectories = { installed.Combine("debug", "lib").ToString() },
				StaticLibraries = debug
			});
		}

		if (release.Count > 0)
		{
			// One artifact per non-Debug configuration: an omitted config would also match Debug
			// and both sets would be linked.
			foreach (var configuration in new[] { "Release", "ReleasePlus", "ReleaseSize" })
			{
				manifest.Binary.Artifacts.Add(new PackageBinaryArtifact
				{
					Config = configuration,
					LibraryDirectories = { installed.Combine("lib").ToString() },
					StaticLibraries = release
				});
			}
			// When vcpkg produced no debug variant, the release one has to serve Debug too or a
			// debug build would link nothing at all.
			if (debug.Count == 0)
			{
				manifest.Binary.Artifacts.Add(new PackageBinaryArtifact
				{
					Config = "Debug",
					LibraryDirectories = { installed.Combine("lib").ToString() },
					StaticLibraries = release
				});
			}
		}

		if (release.Count == 0 && debug.Count == 0)
		{
			Log.Info($"[package] vcpkg port \"{port}\" installed no libraries; treating it as header-only.");
		}

		return JsonConvert.SerializeObject(manifest, Formatting.Indented) + Environment.NewLine;
	}

	internal static bool IsPortInstalled(NPath installed, NPath infoRoot, string port, string triplet)
	{
		return installed.DirectoryExists() && PortInfoFiles(infoRoot, port, triplet).Any();
	}

	private static IEnumerable<NPath> PortInfoFiles(NPath info, string port, string triplet)
	{
		if (!info.DirectoryExists())
		{
			return Array.Empty<NPath>();
		}

		var suffix = $"_{triplet}.list";
		return info.Files("*.list")
			.Where(file => file.FileName.StartsWith($"{port}_", StringComparison.OrdinalIgnoreCase)
			               && file.FileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
			.ToList();
	}

	private static List<string> LibrariesIn(
		NPath directory,
		IEnumerable<string> ownedFiles,
		string relativePrefix)
	{
		if (!directory.DirectoryExists())
		{
			return new List<string>();
		}

		// The triplet's lib directories are shared by every installed port. The .list files under
		// installed/vcpkg/info are vcpkg's ownership records; only entries owned by this port may
		// become link inputs for the synthesized module.
		return ownedFiles
			.Where(path => path.StartsWith(relativePrefix, StringComparison.OrdinalIgnoreCase))
			.Select(path => path.Substring(relativePrefix.Length))
			.Where(path => !path.Contains('/'))
			.Where(path => path.EndsWith(".lib", StringComparison.OrdinalIgnoreCase)
			               || path.EndsWith(".a", StringComparison.OrdinalIgnoreCase))
			.OrderBy(name => name, StringComparer.Ordinal)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	private static void EnsureBootstrapped(FetchRequest request)
	{
		if (!VcpkgRoot.Combine(".git").DirectoryExists())
		{
			Log.Info($"[package] cloning vcpkg {VcpkgToolTag}");
			VcpkgRoot.EnsureParentDirectoryExists();
			// https rather than the ssh remote the old Actions/Vcpkg helper used: a CI runner or a
			// fresh machine has no ssh key and would simply fail.
			ProcessRunner.RunOrThrow(
				"git",
				new[] { "clone", "--branch", VcpkgToolTag, "--depth", "1", VcpkgUrl, VcpkgRoot.ToString() },
				null,
				"cloning vcpkg");
		}

		if (VcpkgExecutable().FileExists())
		{
			return;
		}

		Log.Info("[package] bootstrapping vcpkg");
		if (PlatformHelper.IsWindows())
		{
			// A .bat is not an executable image, so CreateProcess cannot launch it directly and
			// ProcessRunner does not use ShellExecute. The interpreter has to be explicit.
			ProcessRunner.RunOrThrow(
				"cmd.exe",
				new[] { "/c", VcpkgRoot.Combine("bootstrap-vcpkg.bat").ToString() },
				VcpkgRoot,
				"bootstrapping vcpkg");
			return;
		}

		ProcessRunner.RunOrThrow(
			VcpkgRoot.Combine("bootstrap-vcpkg.sh").ToString(),
			Array.Empty<string>(),
			VcpkgRoot,
			"bootstrapping vcpkg");
	}

	private static NPath VcpkgExecutable()
	{
		return VcpkgRoot.Combine(PlatformHelper.IsWindows() ? "vcpkg.exe" : "vcpkg");
	}

	/// <summary>
	/// Falls back to the host's triplet. rbt can cross-compile, and the triplet then has to be
	/// stated explicitly with "triplet" - restore runs before a build context exists, so the
	/// target platform is not knowable here.
	/// </summary>
	public static string DefaultTriplet()
	{
		// Not Is64BitOperatingSystem: that only distinguishes 32- from 64-bit, so every arm64 host
		// would silently be handed an x64 triplet - and rbt targets Apple Silicon and arm64 Linux
		// (Vendor/ninja ships a linux-aarch64 binary, and CI runs a macOS arm64 leg).
		var architecture = RuntimeInformation.OSArchitecture switch
		{
			Architecture.X64 => "x64",
			Architecture.X86 => "x86",
			Architecture.Arm64 => "arm64",
			Architecture.Arm => "arm",
			// Guessing here would install binaries for the wrong machine, which fails far away from
			// the cause. Better to say so and let the user name the triplet.
			var other => throw new PackageException(
				$"no default vcpkg triplet for host architecture {other}. " +
				$"Set \"triplet\" explicitly on the vcpkg dependency.")
		};

		if (PlatformHelper.IsWindows())
		{
			return $"{architecture}-windows";
		}
		if (PlatformHelper.IsOSX())
		{
			return $"{architecture}-osx";
		}
		return $"{architecture}-linux";
	}

	private static void WriteIfChanged(NPath path, string content)
	{
		if (path.FileExists() && path.ReadAllText() == content)
		{
			return;
		}
		path.EnsureParentDirectoryExists();
		path.WriteAllText(content);
	}
}
