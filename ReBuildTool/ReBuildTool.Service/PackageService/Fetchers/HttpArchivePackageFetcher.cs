using NiceIO;
using ReBuildTool.Service.Global;
using ResetCore.Common;

namespace ReBuildTool.Service.PackageService.Fetchers;

/// <summary>
/// Downloads a release archive over HTTP and unpacks it into
/// <c>&lt;ProjectRoot&gt;/Packages/&lt;name&gt;</c>.
///
/// Unlike a git pin, a URL is not self-verifying: the bytes behind it can change without the
/// manifest changing. A <c>sha256</c> in the manifest is therefore how an archive dependency
/// becomes reproducible, and the extracted tree records the hash it was built from so a later
/// restore can tell "already correct" from "the pin moved".
/// </summary>
public class HttpArchivePackageFetcher : IPackageFetcher
{
	/// <summary>
	/// Records which archive the extracted directory came from - the hash on the first line, the
	/// URL it came from on the second. Kept beside the package, not inside it.
	/// </summary>
	private const string StampFileName = ".rbt-archive-sha256";

	public PackageSourceKind Kind => PackageSourceKind.HttpArchive;

	public FetchedPackage Fetch(FetchRequest request)
	{
		var url = request.Dependency.Url!;
		var destination = request.DefaultDestination;
		var stamp = request.SidecarFile(StampFileName);
		var expected = request.Dependency.Sha256;

		// Already unpacked from the very archive the manifest asks for: nothing to do, and no
		// reason to touch the network.
		if (!request.Options.Force && destination.DirectoryExists() && stamp.FileExists())
		{
			var (stampedHash, stampedUrl) = ReadStamp(stamp);
			// With a sha256 the hash decides. Without one there is nothing to compare the content
			// against, so the URL has to: otherwise editing the manifest's url would leave the old
			// tree on disk while the lock recorded the new origin.
			var satisfied = expected != null
				? Hashing.Matches(expected, stampedHash)
				: stampedUrl == url;
			if (satisfied)
			{
				return new FetchedPackage(destination, stampedHash);
			}
		}

		if (request.Options.Offline)
		{
			throw new PackageException(
				$"--Offline was requested but package \"{request.Name}\" still has to be downloaded " +
				$"from {url}. Run a restore without --Offline first.");
		}

		var download = request.PackagesRoot.Combine(".downloads", $"{request.Name}-{Path.GetFileName(new Uri(url).LocalPath)}");
		Log.Info($"[package] downloading {request.Name} from {url}");
		try
		{
			Downloader.Download(url, download);
		}
		catch (Exception e) when (e is not PackageException)
		{
			throw new PackageException($"package \"{request.Name}\": {e.Message}", e);
		}

		var actual = Hashing.Sha256Of(download);
		if (expected != null && !Hashing.Matches(expected, actual))
		{
			download.DeleteIfExists();
			throw new PackageException(
				$"package \"{request.Name}\" failed its checksum.{Environment.NewLine}" +
				$"  expected sha256: {expected}{Environment.NewLine}" +
				$"  actual sha256:   {actual}{Environment.NewLine}" +
				$"The bytes at {url} are not the ones this manifest was written against.");
		}

		// Unpack into a scratch directory and swap it in, so an extraction that dies half way
		// cannot leave a partial tree that the check above would later accept as complete.
		var staging = request.PackagesRoot.Combine($".staging-{request.Name}");
		staging.DeleteIfExists(DeleteMode.Normal);
		try
		{
			ArchiveExtractor.Extract(download, staging, request.Dependency.Strip);
			destination.DeleteIfExists(DeleteMode.Normal);
			staging.Move(destination);
		}
		catch (Exception e) when (e is not PackageException)
		{
			throw new PackageException($"package \"{request.Name}\": {e.Message}", e);
		}
		finally
		{
			staging.DeleteIfExists(DeleteMode.Normal);
			download.DeleteIfExists();
		}

		stamp.WriteAllText($"{actual}{Environment.NewLine}{url}{Environment.NewLine}");
		return new FetchedPackage(destination, actual);
	}

	/// <summary>
	/// Reads the hash and origin URL back out of the stamp. A stamp written by an older rbt has
	/// only the hash; it reports an empty URL, which simply makes the no-sha256 fast path miss and
	/// re-download once.
	/// </summary>
	private static (string Hash, string Url) ReadStamp(NPath stamp)
	{
		var lines = stamp.ReadAllLines();
		return (
			lines.Length > 0 ? lines[0].Trim() : string.Empty,
			lines.Length > 1 ? lines[1].Trim() : string.Empty);
	}
}
