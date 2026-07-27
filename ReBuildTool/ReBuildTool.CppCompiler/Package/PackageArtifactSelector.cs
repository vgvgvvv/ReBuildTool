using NiceIO;
using ReBuildTool.Service.PackageService;
using ResetCore.Common;

namespace ReBuildTool.ToolChain.Package;

/// <summary>
/// Fills a synthesized module rule from a binary package's artifact table, choosing the entry that
/// matches what is currently being built.
///
/// The selection happens here, at <c>Setup</c> time, rather than when the rule file is generated.
/// Baking the current platform into the generated source would make its content change with every
/// <c>--TargetPlatform</c> / <c>--BuildConfig</c> switch, and since the rule assembly is rebuilt
/// whenever a rule file's timestamp moves, that would recompile every rule on each such switch.
/// Generated this way the file depends only on the manifest and never churns.
/// </summary>
public static class PackageArtifactSelector
{
	/// <summary>
	/// Reads the package manifest at <paramref name="manifestPath"/> and applies the matching
	/// artifacts to <paramref name="module"/>. Called from the generated rule's Setup.
	/// </summary>
	public static void Apply(CppModuleRule module, ICppBuildContext buildContext, string manifestPath)
	{
		var path = manifestPath.ToNPath();
		if (!path.FileExists())
		{
			Log.Warning($"[package] {module.TargetName}: {path} is gone; the package was probably removed.");
			return;
		}

		var manifest = PackageManifest.Parse(path.ReadAllText(), path);
		var binary = manifest.Binary;
		if (binary == null)
		{
			return;
		}

		var packageRoot = path.Parent;
		foreach (var include in binary.Includes)
		{
			module.PublicIncludePaths.Add(Resolve(packageRoot, include));
		}

		var platform = IPlatformSupport.CurrentTargetPlatform.ToString();
		// Architecture is matched on CommandLineName - the spelling --TargetArch accepts - not on
		// Name, which is the IDE display name ("ARM64" vs "arm64").
		var architecture = buildContext.CurrentBuildOption.Architecture.CommandLineName;
		var configuration = buildContext.CurrentBuildOption.Configuration.ToString();

		var matched = 0;
		foreach (var artifact in binary.Artifacts)
		{
			if (!Matches(artifact.Platform, platform)
			    || !Matches(artifact.Arch, architecture)
			    || !Matches(artifact.Config, configuration))
			{
				continue;
			}
			matched++;

			foreach (var directory in artifact.LibraryDirectories)
			{
				module.PublicLibraryDirectories.Add(Resolve(packageRoot, directory));
			}
			// Library names are passed to the linker as-is: they may be plain names it resolves
			// through the search paths above, so they must not be turned into paths.
			module.PublicStaticLibraries.AddRange(artifact.StaticLibraries);
			module.PublicDynamicLibraries.AddRange(artifact.DynamicLibraries);
			module.PublicDefines.AddRange(artifact.Defines);
		}

		if (matched == 0 && binary.Artifacts.Count > 0)
		{
			// Not fatal: a package may legitimately support only some platforms, and the consuming
			// module can gate itself with IsSupport. But a silent empty link is far worse to debug.
			Log.Warning(
				$"[package] {module.TargetName} ships no prebuilt artifact for " +
				$"{platform}/{architecture}/{configuration}; nothing will be linked from it.");
		}
	}

	/// <summary>A null or empty selector in the manifest means "every value".</summary>
	private static bool Matches(string? declared, string actual)
	{
		return string.IsNullOrWhiteSpace(declared)
		       || string.Equals(declared, actual, StringComparison.OrdinalIgnoreCase);
	}

	private static string Resolve(NPath packageRoot, string path)
	{
		return System.IO.Path.IsPathRooted(path) ? path : packageRoot.Combine(path).ToString();
	}
}
