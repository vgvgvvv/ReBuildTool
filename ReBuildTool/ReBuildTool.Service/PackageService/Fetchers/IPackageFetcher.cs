using NiceIO;

namespace ReBuildTool.Service.PackageService.Fetchers;

public class FetchRequest
{
	public FetchRequest(
		string name,
		PackageDependency dependency,
		NPath declaringDirectory,
		NPath packagesRoot,
		PackageRestoreOptions options,
		LockedPackage? locked)
	{
		Name = name;
		Dependency = dependency;
		DeclaringDirectory = declaringDirectory;
		PackagesRoot = packagesRoot;
		Options = options;
		Locked = locked;
	}

	public string Name { get; }

	public PackageDependency Dependency { get; }

	/// <summary>Directory of the manifest that declared this dependency - relative paths resolve against it.</summary>
	public NPath DeclaringDirectory { get; }

	/// <summary>&lt;ProjectRoot&gt;/Packages.</summary>
	public NPath PackagesRoot { get; }

	public PackageRestoreOptions Options { get; }

	/// <summary>The matching lock entry, when the project already has one.</summary>
	public LockedPackage? Locked { get; }

	public NPath DefaultDestination => PackagesRoot.Combine(Name);
}

public class FetchedPackage
{
	public FetchedPackage(NPath root, string resolved)
	{
		Root = root;
		Resolved = resolved;
	}

	/// <summary>Where the package content lives. For a path dependency this is outside Packages/.</summary>
	public NPath Root { get; }

	/// <summary>
	/// What the pin actually resolved to, and what the lock records: a commit sha for git, an
	/// archive sha256 for a URL, <c>port:triplet</c> for vcpkg. A path dependency reports the path
	/// as declared rather than <see cref="Root"/> - the lock is committed and shared, so it must
	/// not carry a location that is only meaningful on the machine that wrote it.
	/// </summary>
	public string Resolved { get; }
}

public interface IPackageFetcher
{
	PackageSourceKind Kind { get; }

	FetchedPackage Fetch(FetchRequest request);
}
