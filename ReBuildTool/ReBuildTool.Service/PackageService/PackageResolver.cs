using NiceIO;
using ReBuildTool.Service.CompileService;
using ReBuildTool.Service.PackageService.Fetchers;
using ResetCore.Common;

namespace ReBuildTool.Service.PackageService;

/// <summary>
/// Walks the dependency graph depth-first, fetching each package and then reading the manifest it
/// brought with it to discover the next level.
///
/// rbt resolves <em>exact pins only</em>: there is no version-range solving, so two packages that
/// pin the same dependency differently is a hard error the user resolves with an explicit
/// <c>overrides</c> entry. That keeps the algorithm a plain graph walk with no backtracking, and
/// keeps builds reproducible without a solver.
/// </summary>
public class PackageResolver
{
	public PackageResolver(NPath packagesRoot, IEnumerable<IPackageFetcher> fetchers)
	{
		PackagesRoot = packagesRoot;
		Fetchers = fetchers.ToDictionary(fetcher => fetcher.Kind);
	}

	private NPath PackagesRoot { get; }

	private Dictionary<PackageSourceKind, IPackageFetcher> Fetchers { get; }

	private Dictionary<string, ResolvedEntry> Resolved { get; } = new();

	/// <summary>The DFS path currently being expanded, used to name the members of a dependency cycle.</summary>
	private List<string> Visiting { get; } = new();

	private Dictionary<string, PackageDependency> Overrides { get; set; } = new();

	private PackageLockFile? ExistingLock { get; set; }

	private PackageRestoreOptions Options { get; set; } = new();

	private class ResolvedEntry
	{
		public required string PinKey { get; init; }
		public required RestoredPackage Package { get; init; }
		public required LockedPackage Locked { get; init; }
	}

	public PackageRestoreResult Resolve(
		PackageManifest rootManifest,
		NPath rootDirectory,
		PackageLockFile? existingLock,
		PackageRestoreOptions options,
		out PackageLockFile newLock)
	{
		Resolved.Clear();
		Visiting.Clear();
		Overrides = rootManifest.Overrides;
		ExistingLock = existingLock;
		Options = options;

		foreach (var (name, dependency) in rootManifest.Dependencies)
		{
			ResolveOne(name, dependency, rootDirectory);
		}

		newLock = new PackageLockFile
		{
			Packages = Resolved.Values.Select(entry => entry.Locked).ToList()
		};

		// Discovery order, not dependency order: a package is recorded before its own dependencies
		// are walked, so it precedes them here. Nothing downstream needs a topological order - the
		// packages become rule-glob roots, and the lock is sorted by name when it is written.
		return new PackageRestoreResult(Resolved.Values.Select(entry => entry.Package).ToList());
	}

	private void ResolveOne(string name, PackageDependency declared, NPath declaringDirectory)
	{
		// Checked here rather than in each fetcher: a package fetched from a remote declares its
		// own dependencies, so every name reaching this walk - not just the ones in the project's
		// own manifest - becomes a directory under Packages/.
		PackageNames.ValidatePackageName(name);

		var dependency = Overrides.TryGetValue(name, out var overridden) ? overridden : declared;
		var pinKey = dependency.PinKey(name);

		// Cycle check first: a package is recorded in Resolved before its own dependencies are
		// walked (so that a diamond is fetched once), which means an ancestor looks "already
		// resolved" too. Only Visiting distinguishes "seen before" from "currently on the stack".
		if (Visiting.Contains(name))
		{
			var cycle = string.Join(" -> ", Visiting.Concat(new[] { name }));
			throw new PackageException($"dependency cycle between packages: {cycle}");
		}

		if (Resolved.TryGetValue(name, out var already))
		{
			if (already.PinKey != pinKey)
			{
				throw new PackageException(
					$"conflicting pins for package \"{name}\":{Environment.NewLine}" +
					$"  {already.PinKey}{Environment.NewLine}" +
					$"  {pinKey}{Environment.NewLine}" +
					$"rbt does not pick a version for you. Add an \"overrides\" entry for \"{name}\" " +
					$"in the project's {PackageManifest.FileName} to say which one wins.");
			}
			return;
		}

		var kind = dependency.ResolveKind(name);
		if (!Fetchers.TryGetValue(kind, out var fetcher))
		{
			throw new PackageException(
				$"package \"{name}\" uses the {kind} source, which this build of rbt cannot fetch yet.");
		}

		Visiting.Add(name);
		try
		{
			var request = new FetchRequest(
				name,
				dependency,
				declaringDirectory,
				PackagesRoot,
				Options,
				LockedFor(name, pinKey));
			var fetched = fetcher.Fetch(request);
			var manifest = PackageManifest.ReadFrom(fetched.Root);

			// Recorded before descending so a cycle back to this package is caught by Visiting
			// rather than by re-fetching.
			var locked = new LockedPackage
			{
				Name = name,
				Source = kind.ToString(),
				Origin = dependency.Git ?? dependency.Url ?? dependency.Path ?? dependency.Vcpkg,
				Resolved = fetched.Resolved,
				Pin = pinKey,
				Dependencies = manifest?.Dependencies.Keys.OrderBy(key => key, StringComparer.Ordinal).ToList()
				               ?? new List<string>()
			};
			Resolved[name] = new ResolvedEntry
			{
				PinKey = pinKey,
				Package = new RestoredPackage(
					name,
					fetched.Root,
					manifest,
					ResolveOverlay(name, dependency, declaringDirectory)),
				Locked = locked
			};

			if (manifest != null)
			{
				foreach (var (childName, childDependency) in manifest.Dependencies)
				{
					if (childName == name)
					{
						throw new PackageException($"package \"{name}\" depends on itself.");
					}
					ResolveOne(childName, childDependency, fetched.Root);
				}
			}
		}
		finally
		{
			Visiting.Remove(name);
		}

		Log.Info($"[package] {name} -> {Resolved[name].Locked.Resolved}");
	}

	/// <summary>
	/// The lock entry for a package, but only when it was produced from the pin currently being
	/// resolved.
	///
	/// A fetcher treats the entry as "what this pin resolved to last time" and may reuse it instead
	/// of consulting the remote - that is what keeps an ordinary build reproducible and offline.
	/// Handing over an entry from a different pin turns that shortcut into a trap: bumping a
	/// dependency's tag in the manifest would resolve to the commit the *old* tag pointed at, and
	/// the build would silently stay on the previous version.
	/// </summary>
	private LockedPackage? LockedFor(string name, string pinKey)
	{
		var locked = ExistingLock?.Find(name);
		return locked?.Pin == pinKey ? locked : null;
	}

	/// <summary>
	/// Resolves a dependency's <c>overlay</c> against the manifest that declared it - the rule file
	/// belongs to whoever is consuming the package, not to the package itself.
	/// </summary>
	private static NPath? ResolveOverlay(string name, PackageDependency dependency, NPath declaringDirectory)
	{
		if (string.IsNullOrWhiteSpace(dependency.Overlay))
		{
			return null;
		}

		var overlay = System.IO.Path.IsPathRooted(dependency.Overlay)
			? dependency.Overlay.ToNPath()
			: declaringDirectory.Combine(dependency.Overlay);
		if (!overlay.FileExists())
		{
			throw new PackageException(
				$"package \"{name}\" declares overlay \"{dependency.Overlay}\", which resolves to " +
				$"\"{overlay}\" - that file does not exist.");
		}
		if (!overlay.FileName.EndsWith(ICppProject.ModuleDefineExtension, StringComparison.OrdinalIgnoreCase))
		{
			throw new PackageException(
				$"package \"{name}\": overlay \"{dependency.Overlay}\" must be a " +
				$"{ICppProject.ModuleDefineExtension} file.");
		}
		return overlay;
	}
}
