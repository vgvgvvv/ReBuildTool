using System.Formats.Tar;
using System.IO.Compression;
using NiceIO;

namespace ReBuildTool.Service.Global;

/// <summary>
/// Unpacks the archive formats upstream projects publish releases as. Uses only what .NET 8 ships
/// (<see cref="ZipFile"/>, <see cref="GZipStream"/>, <see cref="TarReader"/>) so no new dependency
/// enters the build.
/// </summary>
public static class ArchiveExtractor
{
	/// <summary>
	/// Extracts <paramref name="archive"/> into <paramref name="destination"/>.
	///
	/// <paramref name="stripComponents"/> drops that many leading path segments from every entry,
	/// like <c>tar --strip-components</c>: release tarballs almost always wrap their contents in a
	/// single <c>name-version/</c> directory that nobody wants in the extracted tree.
	/// </summary>
	public static void Extract(NPath archive, NPath destination, int stripComponents = 0)
	{
		destination.EnsureDirectoryExists();
		var name = archive.FileName.ToLowerInvariant();

		if (name.EndsWith(".zip"))
		{
			ExtractZip(archive, destination, stripComponents);
			return;
		}
		if (name.EndsWith(".tar.gz") || name.EndsWith(".tgz"))
		{
			using var file = File.OpenRead(archive.ToString());
			using var gzip = new GZipStream(file, CompressionMode.Decompress);
			ExtractTar(gzip, destination, stripComponents);
			return;
		}
		if (name.EndsWith(".tar"))
		{
			using var file = File.OpenRead(archive.ToString());
			ExtractTar(file, destination, stripComponents);
			return;
		}

		throw new NotSupportedException(
			$"cannot extract \"{archive.FileName}\": expected a .zip, .tar, .tar.gz or .tgz archive.");
	}

	private static void ExtractZip(NPath archive, NPath destination, int stripComponents)
	{
		using var zip = ZipFile.OpenRead(archive.ToString());
		foreach (var entry in zip.Entries)
		{
			// A zip directory entry has an empty name and no content.
			if (string.IsNullOrEmpty(entry.Name))
			{
				continue;
			}
			var target = ResolveEntryPath(entry.FullName, destination, stripComponents);
			if (target == null)
			{
				continue;
			}
			target.EnsureParentDirectoryExists();
			entry.ExtractToFile(target.ToString(), true);
		}
	}

	private static void ExtractTar(Stream stream, NPath destination, int stripComponents)
	{
		using var reader = new TarReader(stream);
		while (reader.GetNextEntry() is { } entry)
		{
			var target = ResolveEntryPath(entry.Name, destination, stripComponents);
			if (target == null)
			{
				continue;
			}

			if (entry.EntryType is TarEntryType.Directory)
			{
				target.EnsureDirectoryExists();
				continue;
			}
			if (entry.EntryType is not (TarEntryType.RegularFile or TarEntryType.V7RegularFile))
			{
				// Symlinks, devices and hard links are not something a source package needs, and
				// extracting them safely is a different problem. Skip rather than half-support.
				continue;
			}

			target.EnsureParentDirectoryExists();
			entry.ExtractToFile(target.ToString(), true);

			// Packages ship helper scripts and prebuilt tools; losing the executable bit would
			// make them unusable on Unix, and zip has no mode to preserve in the first place.
			if (!OperatingSystem.IsWindows())
			{
				// Owner read/write is forced on: an archive claiming mode 0 would otherwise
				// produce a file rbt cannot read back, let alone delete.
				File.SetUnixFileMode(
					target.ToString(),
					entry.Mode | UnixFileMode.UserRead | UnixFileMode.UserWrite);
			}
		}
	}

	/// <summary>
	/// Maps an archive entry name to a path inside <paramref name="destination"/>, or null when the
	/// entry is stripped away entirely.
	///
	/// Rejects any entry that would escape the destination - the "zip slip" trap, where an archive
	/// carries a name like <c>../../etc/cron.d/x</c> and extraction quietly writes outside the tree
	/// it was told to write into.
	/// </summary>
	private static NPath? ResolveEntryPath(string entryName, NPath destination, int stripComponents)
	{
		var segments = entryName
			.Replace('\\', '/')
			.Split('/', StringSplitOptions.RemoveEmptyEntries)
			.ToList();

		if (segments.Any(segment => segment == ".."))
		{
			throw new IOException(
				$"refusing to extract \"{entryName}\": the archive entry escapes the destination directory.");
		}

		if (stripComponents > 0)
		{
			if (segments.Count <= stripComponents)
			{
				return null;
			}
			segments = segments.Skip(stripComponents).ToList();
		}

		if (segments.Count == 0)
		{
			return null;
		}

		var target = destination.Combine(string.Join("/", segments));
		if (!target.IsChildOf(destination))
		{
			throw new IOException(
				$"refusing to extract \"{entryName}\": it resolves outside the destination directory.");
		}
		return target;
	}
}
