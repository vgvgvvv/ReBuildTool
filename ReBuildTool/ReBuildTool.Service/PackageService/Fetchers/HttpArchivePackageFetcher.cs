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
	/// <summary>Records which archive the extracted directory came from. Kept beside the package, not inside it.</summary>
	private const string StampFileName = ".rbt-archive-sha256";

	public PackageSourceKind Kind => PackageSourceKind.HttpArchive;

	public FetchedPackage Fetch(FetchRequest request)
	{
		var url = request.Dependency.Url!;
		var destination = request.DefaultDestination;
		var stamp = request.PackagesRoot.Combine($"{request.Name}{StampFileName}");
		var expected = request.Dependency.Sha256;

		// Already unpacked from the very archive the manifest asks for: nothing to do, and no
		// reason to touch the network.
		if (!request.Options.Force && destination.DirectoryExists() && stamp.FileExists())
		{
			var current = stamp.ReadAllText().Trim();
			if (expected == null || Hashing.Matches(expected, current))
			{
                return new FetchedPackage(destination, current);
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

		stamp.WriteAllText(actual);
		return new FetchedPackage(destination, actual);
	}
}
