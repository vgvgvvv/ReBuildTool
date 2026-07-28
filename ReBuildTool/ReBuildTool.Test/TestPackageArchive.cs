using System.Formats.Tar;
using System.IO.Compression;
using System.Net;
using NiceIO;
using ReBuildTool.Service.Global;
using ReBuildTool.Service.PackageService;

namespace ReBuildTool.Test;

/// <summary>
/// Archive extraction and the HTTP package source.
///
/// The archives are built by the test and served from a loopback <see cref="HttpListener"/>, so
/// the real download / checksum / unpack path runs end to end without depending on any external
/// host being up.
/// </summary>
[TestFixture]
public class TestPackageArchive
{
    private NPath WorkDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        WorkDirectory = Path.Combine(Path.GetTempPath(), $"rbt-archive-{Guid.NewGuid():N}")
            .ToNPath()
            .EnsureDirectoryExists();
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            WorkDirectory.DeleteIfExists(DeleteMode.Normal);
        }
        catch (Exception)
        {
            // A leftover temp directory must never fail a test run.
        }
    }

    /// <summary>Builds a zip whose entries are all under a single top-level directory.</summary>
    private NPath CreateZip(string archiveName, string topLevel, params (string Path, string Content)[] entries)
    {
        var archive = WorkDirectory.Combine(archiveName);
        using var stream = File.Create(archive.ToString());
        using var zip = new ZipArchive(stream, ZipArchiveMode.Create);
        foreach (var (path, content) in entries)
        {
            var entry = zip.CreateEntry($"{topLevel}/{path}");
            using var writer = new StreamWriter(entry.Open());
            writer.Write(content);
        }
        return archive;
    }

    private NPath CreateTarGz(string archiveName, string topLevel, params (string Path, string Content)[] entries)
    {
        var staging = WorkDirectory.Combine($"staging-{Guid.NewGuid():N}").EnsureDirectoryExists();
        foreach (var (path, content) in entries)
        {
            var file = staging.Combine(topLevel, path);
            file.EnsureParentDirectoryExists();
            file.WriteAllText(content);
        }

        var tar = WorkDirectory.Combine($"{archiveName}.tar");
        TarFile.CreateFromDirectory(staging.ToString(), tar.ToString(), false);

        var archive = WorkDirectory.Combine(archiveName);
        using (var input = File.OpenRead(tar.ToString()))
        using (var output = File.Create(archive.ToString()))
        using (var gzip = new GZipStream(output, CompressionMode.Compress))
        {
            input.CopyTo(gzip);
        }
        tar.DeleteIfExists();
        staging.DeleteIfExists(DeleteMode.Normal);
        return archive;
    }

    /// <summary>Serves a single file on loopback for the lifetime of the returned disposable.</summary>
    private sealed class LocalServer : IDisposable
    {
        private readonly HttpListener Listener;

        public LocalServer(NPath file)
        {
            // Port 0 is not available through HttpListener, so probe upward for a free one. The
            // probe writes to a local and the fields are assigned once, so neither is ever observed
            // half-initialized.
            HttpListener? started = null;
            var url = string.Empty;
            for (var port = 18800; port < 18900 && started == null; port++)
            {
                var listener = new HttpListener();
                listener.Prefixes.Add($"http://127.0.0.1:{port}/");
                try
                {
                    listener.Start();
                    started = listener;
                    url = $"http://127.0.0.1:{port}/{file.FileName}";
                }
                catch (HttpListenerException)
                {
                    listener.Close();
                }
            }

            Listener = started
                       ?? throw new InvalidOperationException("no free loopback port for the test server");
            Url = url;

            var bytes = File.ReadAllBytes(file.ToString());
            Task.Run(() =>
            {
                while (Listener.IsListening)
                {
                    HttpListenerContext context;
                    try
                    {
                        context = Listener.GetContext();
                    }
                    catch (Exception)
                    {
                        return;
                    }
                    context.Response.ContentLength64 = bytes.Length;
                    context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                    context.Response.OutputStream.Close();
                }
            });
        }

        public string Url { get; }

        public void Dispose()
        {
            Listener.Stop();
            Listener.Close();
        }
    }

    private NPath CreateProject(string manifestJson)
    {
        var project = WorkDirectory.Combine("Project").EnsureDirectoryExists();
        PackageManifest.PathIn(project).WriteAllText(manifestJson);
        return project;
    }

    [Test]
    public void StripComponentsDropsTheWrapperDirectory()
    {
        var archive = CreateZip("lib.zip", "libfoo-1.2.3", ("include/foo.h", "#pragma once"));
        var destination = WorkDirectory.Combine("out");

        ArchiveExtractor.Extract(archive, destination, 1);

        // Without the strip the header would land under out/libfoo-1.2.3/include, which is not
        // what any manifest wants to write include paths against.
        Assert.That(destination.Combine("include", "foo.h").FileExists(), Is.True);
        Assert.That(destination.Combine("libfoo-1.2.3").DirectoryExists(), Is.False);
    }

    [Test]
    public void TarGzIsExtracted()
    {
        var archive = CreateTarGz("lib.tar.gz", "libfoo-1.2.3", ("include/foo.h", "#pragma once"));
        var destination = WorkDirectory.Combine("out");

        ArchiveExtractor.Extract(archive, destination, 1);

        Assert.That(destination.Combine("include", "foo.h").FileExists(), Is.True);
    }

    /// <summary>
    /// The "zip slip" trap: an archive entry whose name climbs out of the destination. Extracting
    /// it would write wherever the attacker named, so it has to be refused outright.
    /// </summary>
    [Test]
    public void AnEntryEscapingTheDestinationIsRefused()
    {
        var archive = WorkDirectory.Combine("evil.zip");
        using (var stream = File.Create(archive.ToString()))
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create))
        {
            var entry = zip.CreateEntry("../escaped.txt");
            using var writer = new StreamWriter(entry.Open());
            writer.Write("pwned");
        }

        var destination = WorkDirectory.Combine("out");
        Assert.Throws<IOException>(() => ArchiveExtractor.Extract(archive, destination, 0));
        Assert.That(WorkDirectory.Combine("escaped.txt").FileExists(), Is.False);
    }

    [Test]
    public void AnArchivePackageIsDownloadedAndUnpacked()
    {
        var archive = CreateZip("geo.zip", "geo-1.0",
            ("Geo.module.cs", "using ReBuildTool.ToolChain; public class Geo : CppModuleRule { }"),
            (PackageManifest.FileName, "{ \"name\": \"Geo\" }"));
        var sha = Hashing.Sha256Of(archive);
        using var server = new LocalServer(archive);
        var project = CreateProject(
            "{ \"dependencies\": { \"Geo\": { " +
            $"\"url\": \"{server.Url}\", \"sha256\": \"{sha}\", \"strip\": 1 }} }} }}");

        var result = new PackageRestoreService().Restore(project, new PackageRestoreOptions());

        Assert.That(result.Packages.Select(package => package.Name), Is.EqualTo(new[] { "Geo" }));
        Assert.That(project.Combine("Packages", "Geo", "Geo.module.cs").FileExists(), Is.True);
        Assert.That(PackageLockFile.ReadFrom(project)!.Find("Geo")!.Resolved, Is.EqualTo(sha));
    }

    [Test]
    public void AChecksumMismatchIsFatalAndExplainsItself()
    {
        var archive = CreateZip("geo.zip", "geo-1.0", ("Geo.module.cs", "// content"));
        using var server = new LocalServer(archive);
        var wrong = new string('a', 64);
        var project = CreateProject(
            "{ \"dependencies\": { \"Geo\": { " +
            $"\"url\": \"{server.Url}\", \"sha256\": \"{wrong}\", \"strip\": 1 }} }} }}");

        var exception = Assert.Throws<PackageException>(
            () => new PackageRestoreService().Restore(project, new PackageRestoreOptions()));

        // The message has to show both hashes; "checksum failed" alone tells the user nothing.
        Assert.That(exception!.Message, Does.Contain(wrong));
        Assert.That(exception.Message, Does.Contain(Hashing.Sha256Of(archive)));
        Assert.That(project.Combine("Packages", "Geo").DirectoryExists(), Is.False);
    }

    /// <summary>
    /// Without a sha256 there is nothing to compare the unpacked tree against, so the cache hit has
    /// to be keyed on the URL. Otherwise editing the manifest's url leaves the previous archive on
    /// disk while the lock records the new origin - stale content under a fresh-looking pin.
    /// </summary>
    [Test]
    public void ChangingTheUrlWithNoChecksumStillReplacesTheContent()
    {
        var first = CreateZip("first.zip", "pkg-1.0", ("first.txt", "1"));
        var second = CreateZip("second.zip", "pkg-2.0", ("second.txt", "2"));

        NPath project;
        using (var server = new LocalServer(first))
        {
            project = CreateProject(
                "{ \"dependencies\": { \"Geo\": { " +
                $"\"url\": \"{server.Url}\", \"strip\": 1 }} }} }}");
            new PackageRestoreService().Restore(project, new PackageRestoreOptions());
        }
        Assert.That(project.Combine("Packages", "Geo", "first.txt").FileExists(), Is.True);

        using (var server = new LocalServer(second))
        {
            PackageManifest.PathIn(project).WriteAllText(
                "{ \"dependencies\": { \"Geo\": { " +
                $"\"url\": \"{server.Url}\", \"strip\": 1 }} }} }}");
            new PackageRestoreService().Restore(project, new PackageRestoreOptions());
        }

        Assert.That(project.Combine("Packages", "Geo", "second.txt").FileExists(), Is.True,
            "the new archive should have been fetched and unpacked");
        Assert.That(project.Combine("Packages", "Geo", "first.txt").FileExists(), Is.False,
            "the previous archive's content should be gone");
    }

    [Test]
    public void ASecondRestoreOfAnArchivePackageNeedsNoNetwork()
    {
        var archive = CreateZip("geo.zip", "geo-1.0", (PackageManifest.FileName, "{ \"name\": \"Geo\" }"));
        var sha = Hashing.Sha256Of(archive);
        var manifest =
            "{ \"dependencies\": { \"Geo\": { " +
            $"\"url\": \"http://127.0.0.1:18999/geo.zip\", \"sha256\": \"{sha}\", \"strip\": 1 }} }} }}";

        NPath project;
        using (var server = new LocalServer(archive))
        {
            project = CreateProject(
                "{ \"dependencies\": { \"Geo\": { " +
                $"\"url\": \"{server.Url}\", \"sha256\": \"{sha}\", \"strip\": 1 }} }} }}");
            new PackageRestoreService().Restore(project, new PackageRestoreOptions());
        }

        // The server is gone; the already-unpacked tree must be recognised by its recorded hash.
        PackageManifest.PathIn(project).WriteAllText(manifest);
        Assert.DoesNotThrow(() =>
            new PackageRestoreService().Restore(project, new PackageRestoreOptions { Offline = true }));
    }
}
