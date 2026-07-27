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
		var triplet = request.Dependency.Triplet ?? DefaultTriplet();
		var installed = VcpkgRoot.Combine("installed", triplet);
		var destination = request.DefaultDestination;

		var alreadyInstalled = installed.DirectoryExists()
		                       && VcpkgRoot.Combine("installed", "vcpkg", "info").DirectoryExists();

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
		var release = LibrariesIn(installed.Combine("lib"));
		var debug = LibrariesIn(installed.Combine("debug", "lib"));

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

	private static List<string> LibrariesIn(NPath directory)
	{
		if (!directory.DirectoryExists())
		{
			return new List<string>();
		}
		return directory.Files()
			.Where(file => file.ExtensionWithDot is ".lib" or ".a")
			.Select(file => file.FileName)
			.OrderBy(name => name, StringComparer.Ordinal)
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

		var bootstrap = VcpkgRoot.Combine(
			PlatformHelper.IsWindows() ? "bootstrap-vcpkg.bat" : "bootstrap-vcpkg.sh");
		Log.Info("[package] bootstrapping vcpkg");
		ProcessRunner.RunOrThrow(bootstrap.ToString(), Array.Empty<string>(), VcpkgRoot, "bootstrapping vcpkg");
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
		var architecture = Environment.Is64BitOperatingSystem ? "x64" : "x86";
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
