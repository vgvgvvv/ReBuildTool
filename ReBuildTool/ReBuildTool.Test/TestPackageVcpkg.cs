using NiceIO;
using ReBuildTool.Service.PackageService;
using ReBuildTool.Service.PackageService.Fetchers;

namespace ReBuildTool.Test;

/// <summary>
/// The vcpkg bridge's translation step: turning an installed tree into a binary-package manifest.
///
/// Running vcpkg itself needs a large clone, a bootstrap and a network, none of which belongs in a
/// test suite that has to pass on three CI hosts. The mapping is where the decisions live - vcpkg's
/// debug/release split in particular - so the installed tree is faked and only the mapping is
/// exercised.
/// </summary>
[TestFixture]
public class TestPackageVcpkg
{
    private NPath WorkDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        WorkDirectory = Path.Combine(Path.GetTempPath(), $"rbt-vcpkg-{Guid.NewGuid():N}")
            .ToNPath()
            .EnsureDirectoryExists();
    }

    [TearDown]
    public void TearDown()
    {
        WorkDirectory.DeleteIfExists(DeleteMode.Normal);
    }

    /// <summary>Fakes the layout vcpkg produces under installed/&lt;triplet&gt;.</summary>
    private NPath FakeInstalledTree(string[] releaseLibraries, string[] debugLibraries)
    {
        var installed = WorkDirectory.Combine("installed", "x64-linux");
        installed.Combine("include").EnsureDirectoryExists().Combine("thing.h").WriteAllText("#pragma once");
        foreach (var library in releaseLibraries)
        {
            installed.Combine("lib").EnsureDirectoryExists().Combine(library).WriteAllText("");
        }
        foreach (var library in debugLibraries)
        {
            installed.Combine("debug", "lib").EnsureDirectoryExists().Combine(library).WriteAllText("");
        }
        return installed;
    }

    private static PackageManifest Describe(string name, NPath installed)
    {
        var json = VcpkgPackageFetcher.DescribeInstalledTree(name, "someport", installed);
        return PackageManifest.Parse(json, "generated".ToNPath());
    }

    [Test]
    public void TheIncludeDirectoryBecomesTheModulesPublicInclude()
    {
        var installed = FakeInstalledTree(new[] { "libthing.a" }, Array.Empty<string>());

        var manifest = Describe("Thing", installed);

        Assert.That(manifest.Binary, Is.Not.Null);
        Assert.That(manifest.Binary!.Module, Is.EqualTo("Thing"));
        Assert.That(manifest.Binary.Includes.Single(), Is.EqualTo(installed.Combine("include").ToString()));
    }

    /// <summary>
    /// vcpkg builds both variants into parallel prefixes. Linking the release libraries into a
    /// Debug build (or both sets at once) is exactly the mistake this split exists to avoid.
    /// </summary>
    [Test]
    public void DebugAndReleaseLibrariesMapToTheirOwnConfigurations()
    {
        var installed = FakeInstalledTree(new[] { "libthing.a" }, new[] { "libthingd.a" });

        var manifest = Describe("Thing", installed);

        var debug = manifest.Binary!.Artifacts.Single(artifact => artifact.Config == "Debug");
        Assert.That(debug.StaticLibraries, Is.EqualTo(new[] { "libthingd.a" }));
        Assert.That(debug.LibraryDirectories.Single(),
            Is.EqualTo(installed.Combine("debug", "lib").ToString()));

        var release = manifest.Binary.Artifacts.Single(artifact => artifact.Config == "Release");
        Assert.That(release.StaticLibraries, Is.EqualTo(new[] { "libthing.a" }));
        Assert.That(release.LibraryDirectories.Single(), Is.EqualTo(installed.Combine("lib").ToString()));
    }

    [Test]
    public void EveryReleaseConfigurationIsCovered()
    {
        var installed = FakeInstalledTree(new[] { "libthing.a" }, new[] { "libthingd.a" });

        var manifest = Describe("Thing", installed);

        // An artifact with no config would also match Debug, so each one is named explicitly.
        Assert.That(
            manifest.Binary!.Artifacts.Select(artifact => artifact.Config).OrderBy(config => config),
            Is.EqualTo(new[] { "Debug", "Release", "ReleasePlus", "ReleaseSize" }));
    }

    /// <summary>
    /// Plenty of ports build only a release variant. Leaving Debug uncovered would link nothing at
    /// all in a debug build, which is far worse than using the release libraries.
    /// </summary>
    [Test]
    public void ReleaseLibrariesServeDebugWhenThePortHasNoDebugBuild()
    {
        var installed = FakeInstalledTree(new[] { "libthing.a" }, Array.Empty<string>());

        var manifest = Describe("Thing", installed);

        var debug = manifest.Binary!.Artifacts.Single(artifact => artifact.Config == "Debug");
        Assert.That(debug.StaticLibraries, Is.EqualTo(new[] { "libthing.a" }));
        Assert.That(debug.LibraryDirectories.Single(), Is.EqualTo(installed.Combine("lib").ToString()));
    }

    [Test]
    public void AHeaderOnlyPortProducesNoArtifacts()
    {
        var installed = FakeInstalledTree(Array.Empty<string>(), Array.Empty<string>());

        var manifest = Describe("Thing", installed);

        Assert.That(manifest.Binary!.Artifacts, Is.Empty);
        Assert.That(manifest.Binary.Includes, Is.Not.Empty);
    }

    [Test]
    public void NonLibraryFilesAreNotLinked()
    {
        var installed = FakeInstalledTree(new[] { "libthing.a" }, Array.Empty<string>());
        // vcpkg drops pkg-config and cmake config files in lib/ alongside the real libraries.
        installed.Combine("lib", "thing.pc").WriteAllText("");
        installed.Combine("lib", "pkgconfig").EnsureDirectoryExists();

        var manifest = Describe("Thing", installed);

        Assert.That(
            manifest.Binary!.Artifacts.SelectMany(artifact => artifact.StaticLibraries).Distinct(),
            Is.EqualTo(new[] { "libthing.a" }));
    }

    [Test]
    public void TheHostTripletIsUsedWhenNoneIsDeclared()
    {
        var triplet = VcpkgPackageFetcher.DefaultTriplet();

        Assert.That(triplet, Does.Match(@"^(x64|x86)-(windows|osx|linux)$"));
    }

    /// <summary>
    /// An omitted triplet means the host's, so it has to be resolved before the pins are compared -
    /// otherwise these two spellings of the same thing would be reported as a conflict on the very
    /// machine where they are identical.
    /// </summary>
    [Test]
    public void AnOmittedTripletPinsTheSameAsTheExplicitHostTriplet()
    {
        var host = VcpkgPackageFetcher.DefaultTriplet();
        var manifest = PackageManifest.Parse(
            "{ \"dependencies\": { \"implicit\": { \"vcpkg\": \"fmt\" }, " +
            $"\"explicit\": {{ \"vcpkg\": \"fmt\", \"triplet\": \"{host}\" }} }} }}",
            "test".ToNPath());

        Assert.That(
            manifest.Dependencies["implicit"].PinKey("fmt"),
            Is.EqualTo(manifest.Dependencies["explicit"].PinKey("fmt")));
        // And the recorded pin has to name the triplet that was actually built.
        Assert.That(manifest.Dependencies["implicit"].PinKey("fmt"), Does.Contain(host));
    }

    /// <summary>
    /// The pin ends up in the lock and in conflict messages, so it should read as the thing it
    /// identifies - port and triplet, with no empty field standing in for a version rbt does not
    /// support pinning (which vcpkg checkout you get is fixed by the pinned vcpkg tag instead).
    /// </summary>
    [Test]
    public void TheVcpkgPinReadsAsPortAndTriplet()
    {
        var manifest = PackageManifest.Parse(
            "{ \"dependencies\": { \"fmt\": { \"vcpkg\": \"fmt\", \"triplet\": \"x64-windows\" } } }",
            "test".ToNPath());

        Assert.That(manifest.Dependencies["fmt"].PinKey("fmt"), Is.EqualTo("vcpkg:fmt:x64-windows"));
    }

    [Test]
    public void TheTripletIsPartOfThePin()
    {
        var manifest = PackageManifest.Parse(
            "{ \"dependencies\": { \"fmt\": { \"vcpkg\": \"fmt\", \"triplet\": \"x64-windows\" }, " +
            "\"fmt2\": { \"vcpkg\": \"fmt\", \"triplet\": \"arm64-osx\" } } }",
            "test".ToNPath());

        // Two triplets of one port are genuinely different content, so they must not look
        // interchangeable to the conflict check.
        Assert.That(
            manifest.Dependencies["fmt"].PinKey("fmt"),
            Is.Not.EqualTo(manifest.Dependencies["fmt2"].PinKey("fmt")));
    }
}
