using NiceIO;

namespace ReBuildTool.Service.PackageService.Fetchers;

/// <summary>
/// A dependency on a directory already present on this machine.
///
/// Nothing is copied: the package is used where it lies, so edits in the depended-on source show
/// up in the very next build. That is the point of a path dependency - local co-development of a
/// library and its consumer.
/// </summary>
public class PathPackageFetcher : IPackageFetcher
{
	public PackageSourceKind Kind => PackageSourceKind.Path;

	public FetchedPackage Fetch(FetchRequest request)
	{
		var declared = request.Dependency.Path!;
		var resolved = System.IO.Path.IsPathRooted(declared)
			? declared.ToNPath()
			: request.DeclaringDirectory.Combine(declared);

		// MakeAbsolute collapses the ".." that a sibling-directory dependency almost always uses,
		// so the lock records a stable path rather than one relative to the declaring manifest.
		resolved = resolved.MakeAbsolute();

		if (!resolved.DirectoryExists())
		{
			throw new PackageException(
				$"path dependency \"{request.Name}\" points at \"{declared}\", which resolves to " +
				$"\"{resolved}\" - that directory does not exist.");
		}

		// The lock records the path as it was declared, not where it landed on this machine: an
		// absolute path is derivable from the manifest anyway, and committing one would make the
		// lock file useless to every other checkout.
		return new FetchedPackage(resolved, declared);
	}
}
