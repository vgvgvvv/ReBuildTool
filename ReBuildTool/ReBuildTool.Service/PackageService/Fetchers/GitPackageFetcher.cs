using NiceIO;
using ResetCore.Common;

namespace ReBuildTool.Service.PackageService.Fetchers;

/// <summary>
/// Clones a git dependency into <c>&lt;ProjectRoot&gt;/Packages/&lt;name&gt;</c> and parks it at an
/// exact commit.
///
/// The clone keeps its <c>.git</c> directory so a later restore updates with a fetch instead of
/// re-downloading, and so a moving pin (tag or branch) can be re-resolved on <c>--UpdateLock</c>.
/// Whatever the pin was written as, the lock always records the commit it resolved to - tags get
/// moved upstream, commits do not.
/// </summary>
public class GitPackageFetcher : IPackageFetcher
{
	public PackageSourceKind Kind => PackageSourceKind.Git;

	public FetchedPackage Fetch(FetchRequest request)
	{
		var url = request.Dependency.Git!;
		var destination = request.DefaultDestination;
		var isClone = destination.Combine(".git").DirectoryExists();

		if (isClone)
		{
			var existingUrl = ProcessRunner.RunOrThrow(
				"git",
				new[] { "remote", "get-url", "origin" },
				destination,
				$"reading the origin of package \"{request.Name}\"");
			if (!string.Equals(existingUrl, url, StringComparison.Ordinal))
			{
				// Git objects and refs survive a remote URL change. Reusing this clone could
				// therefore resolve a tag or commit from the old repository while the lock claims
				// it came from the new one. A fresh clone is the only reliable way to keep the
				// object database tied to the declared origin.
				RequireNetwork(
					request,
					$"package \"{request.Name}\" changed its git origin from \"{existingUrl}\" to \"{url}\"");
				Log.Info($"[package] origin changed for {request.Name}; cloning it again from {url}");
				DeleteClone(destination);
				isClone = false;
			}
		}

		if (!isClone)
		{
			// A leftover directory that is not a clone (an interrupted fetch, or a rename) would
			// make every git command below fail with a confusing message. Start clean instead.
			if (destination.DirectoryExists())
			{
				destination.DeleteIfExists(DeleteMode.Normal);
			}
			RequireNetwork(request, $"package \"{request.Name}\" has not been cloned yet");
			Log.Info($"[package] cloning {request.Name} from {url}");
			destination.EnsureParentDirectoryExists();
			ProcessRunner.RunOrThrow(
				"git",
				// "--" so the URL cannot be read as an option. The manifest validation already
				// rejects a leading '-', but the marker costs nothing and does not rely on that
				// check staying in place.
				new[] { "clone", "--recurse-submodules", "--", url, destination.ToString() },
				null,
				$"cloning package \"{request.Name}\"");
		}

		var revision = ResolveRevision(request, destination);

		// Restore runs before every build, so the already-correct case has to be free. Checking
		// HEAD costs one git call; resetting unconditionally would rewrite the whole work tree
		// (and bump every source file's timestamp, forcing a full recompile) on each build.
		if (TryResolve(destination, "HEAD") != revision)
		{
			ProcessRunner.RunOrThrow(
				"git",
				new[] { "reset", "--hard", revision },
				destination,
				$"checking out package \"{request.Name}\" at {revision}");
			ProcessRunner.RunOrThrow(
				"git",
				new[] { "submodule", "update", "--init", "--recursive" },
				destination,
				$"updating submodules of package \"{request.Name}\"");
		}

		return new FetchedPackage(destination, revision);
	}

	/// <summary>
	/// Turns the declared pin into a concrete commit sha, fetching from the remote only when the
	/// answer is not already available locally (or when the caller asked to re-resolve).
	/// </summary>
	private string ResolveRevision(FetchRequest request, NPath repository)
	{
		var dependency = request.Dependency;

		// An explicit commit is already exact; it just has to be present in the clone.
		if (!string.IsNullOrWhiteSpace(dependency.Commit))
		{
			var local = TryResolve(repository, dependency.Commit);
			if (local != null)
			{
				return local;
			}
			RequireNetwork(request, $"commit {dependency.Commit} of \"{request.Name}\" is not in the local clone");
			FetchRemote(request, repository);
			return TryResolve(repository, dependency.Commit)
			       ?? throw new PackageException(
				       $"package \"{request.Name}\": commit {dependency.Commit} does not exist in {dependency.Git}.");
		}

		// A tag or a branch is a moving target. Re-resolve it when asked to, otherwise reuse what
		// the lock already pinned so a plain build stays reproducible and offline.
		var reference = !string.IsNullOrWhiteSpace(dependency.Tag)
			? $"refs/tags/{dependency.Tag}"
			: $"refs/remotes/origin/{dependency.Branch}";

		if (!request.Options.UpdateLock && request.Locked?.Resolved != null)
		{
			var pinned = TryResolve(repository, request.Locked.Resolved);
			if (pinned != null)
			{
				return pinned;
			}
		}

		if (request.Options.UpdateLock || TryResolve(repository, reference) == null)
		{
			RequireNetwork(request, $"\"{request.Name}\" needs {reference} resolved against the remote");
			FetchRemote(request, repository);
		}

		return TryResolve(repository, reference)
		       ?? throw new PackageException(
			       $"package \"{request.Name}\": {reference} does not exist in {dependency.Git}.");
	}

	private static void FetchRemote(FetchRequest request, NPath repository)
	{
		Log.Info($"[package] fetching {request.Name}");
		ProcessRunner.RunOrThrow(
			"git",
			new[] { "fetch", "--tags", "--force", "origin" },
			repository,
			$"fetching package \"{request.Name}\"");
	}

	/// <summary>Resolves a revision to a commit sha, or null when git does not know it locally.</summary>
	private static string? TryResolve(NPath repository, string reference)
	{
		var result = ProcessRunner.Run(
			"git",
			new[] { "rev-parse", "--verify", "--quiet", $"{reference}^{{commit}}" },
			repository);
		if (!result.IsSuccess)
		{
			return null;
		}
		var sha = result.StdOut.Trim();
		return string.IsNullOrEmpty(sha) ? null : sha;
	}

	private static void RequireNetwork(FetchRequest request, string why)
	{
		if (request.Options.Offline)
		{
			throw new PackageException(
				$"--Offline was requested but {why}. Run a restore without --Offline first.");
		}
	}

	private static void DeleteClone(NPath destination)
	{
		// Git object files can be read-only on Windows. Directory.Delete reports those as
		// UnauthorizedAccessException, so clear only that bit inside the exact package directory
		// before replacing the clone.
		if (OperatingSystem.IsWindows())
		{
			foreach (var file in Directory.EnumerateFiles(
				         destination.ToString(),
				         "*",
				         SearchOption.AllDirectories))
			{
				var attributes = File.GetAttributes(file);
				if ((attributes & FileAttributes.ReadOnly) != 0)
				{
					File.SetAttributes(file, attributes & ~FileAttributes.ReadOnly);
				}
			}
		}
		destination.DeleteIfExists(DeleteMode.Normal);
	}
}
