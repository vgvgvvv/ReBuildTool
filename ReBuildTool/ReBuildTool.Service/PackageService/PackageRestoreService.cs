using NiceIO;
using ReBuildTool.Service.PackageService.Fetchers;
using ResetCore.Common;

namespace ReBuildTool.Service.PackageService;

/// <summary>
/// The default <see cref="IPackageService"/>: reads the project manifest, resolves and fetches the
/// whole dependency graph into <c>&lt;ProjectRoot&gt;/Packages/</c>, and writes the lock.
///
/// Packages deliberately do <em>not</em> live under <c>Intermedia/</c>: <c>CppBuildProject.Clean</c>
/// empties that directory, and <c>CleanIfNeed</c> triggers a clean on its own whenever the rbt
/// binaries are newer than the last build - dependencies would be re-downloaded after every
/// rebuild and after every rbt update.
/// </summary>
public class PackageRestoreService : IPackageService
{
	public const string PackagesFolderName = "Packages";

	public PackageRestoreResult Restore(NPath projectRoot, PackageRestoreOptions options)
	{
		var manifestPath = PackageManifest.PathIn(projectRoot);
		if (!manifestPath.FileExists())
		{
			// No manifest means no package management at all: a project that does not use the
			// feature must not get a Packages/ directory, a lock file or a .gitignore edit.
			return PackageRestoreResult.Empty;
		}

		var manifest = PackageManifest.Parse(manifestPath.ReadAllText(), manifestPath);
		if (manifest.Dependencies.Count == 0)
		{
			return PackageRestoreResult.Empty;
		}

		var packagesRoot = projectRoot.Combine(PackagesFolderName);
		packagesRoot.EnsureDirectoryExists();
		EnsureGitIgnored(projectRoot);

		var existingLock = options.Force ? null : PackageLockFile.ReadFrom(projectRoot);
		var resolver = new PackageResolver(packagesRoot, CreateFetchers());
		var result = resolver.Resolve(manifest, projectRoot, existingLock, options, out var newLock);
		newLock.WriteIfChanged(projectRoot);

		return result;
	}

	private static IEnumerable<IPackageFetcher> CreateFetchers()
	{
		yield return new GitPackageFetcher();
		yield return new PathPackageFetcher();
		yield return new HttpArchivePackageFetcher();
		yield return new VcpkgPackageFetcher();
	}

	/// <summary>
	/// Keeps the materialized <c>Packages/</c> tree out of the consuming repository. Only touches a
	/// .gitignore that already exists or a directory that is actually a git repository, and only
	/// when the pattern is not already there, so it stays a no-op on every subsequent build.
	/// </summary>
	private static void EnsureGitIgnored(NPath projectRoot)
	{
		var ignorePath = projectRoot.Combine(".gitignore");
		if (!ignorePath.FileExists() && !projectRoot.Combine(".git").DirectoryExists())
		{
			return;
		}

		var pattern = $"/{PackagesFolderName}/";
		var lines = ignorePath.FileExists()
			? ignorePath.ReadAllLines().ToList()
			: new List<string>();
		if (lines.Any(line => line.Trim() == pattern || line.Trim() == PackagesFolderName))
		{
			return;
		}

		lines.Add(pattern);
		try
		{
			ignorePath.WriteAllLines(lines.ToArray());
		}
		catch (Exception e)
		{
			// Never fail a build over a convenience edit to a file rbt does not own.
			Log.Warning($"[package] could not add \"{pattern}\" to {ignorePath}: {e.Message}");
		}
	}
}
