using NiceIO;
using ReBuildTool.Service.Context;

namespace ReBuildTool.Service.PackageService;

public class PackageRestoreOptions
{
	/// <summary>Never touch the network: if the lock is not already satisfied on disk, fail.</summary>
	public bool Offline { get; set; }

	/// <summary>Re-fetch every package even when the lock is satisfied.</summary>
	public bool Force { get; set; }

	/// <summary>Re-resolve moving refs (tags/branches) and rewrite the lock, like <c>cargo update</c>.</summary>
	public bool UpdateLock { get; set; }
}

/// <summary>
/// A package that has been materialized on disk and is ready to take part in the build.
/// </summary>
public class RestoredPackage
{
	public RestoredPackage(string name, NPath root, PackageManifest? manifest, NPath? overlay = null)
	{
		Name = name;
		Root = root;
		Manifest = manifest;
		Overlay = overlay;
	}

	public string Name { get; }

	/// <summary>Where the package's content lives - the directory rbt globs rule files out of.</summary>
	public NPath Root { get; }

	public PackageManifest? Manifest { get; }

	/// <summary>
	/// A <c>.module.cs</c> supplied by the consuming project for a package that ships none of its
	/// own - an unmodified upstream source tree. Already resolved to an absolute path.
	/// </summary>
	public NPath? Overlay { get; }
}

public class PackageRestoreResult
{
	public static PackageRestoreResult Empty { get; } = new(new List<RestoredPackage>());

	public PackageRestoreResult(List<RestoredPackage> packages)
	{
		Packages = packages;
	}

	public List<RestoredPackage> Packages { get; }
}

/// <summary>
/// Fetches every package the project's <c>RBTPackage.json</c> transitively depends on and
/// materializes it under <c>&lt;ProjectRoot&gt;/Packages/</c>.
///
/// This has to run before the rule files are globbed and compiled: a package's own
/// <c>.module.cs</c> must already be on disk when <c>CppBuildProject.ParseRules</c> builds the
/// <c>CompileRules.dll</c> compile unit, because that assembly is loaded exactly once with
/// <c>Assembly.LoadFile</c> and cannot be unloaded and rebuilt afterwards.
/// </summary>
public interface IPackageService : IService
{
	PackageRestoreResult Restore(NPath projectRoot, PackageRestoreOptions options);
}
