using NiceIO;
using ReBuildTool.Service.PackageService;
using ReBuildTool.Service.PackageService.Fetchers;

namespace ReBuildTool.Test;

/// <summary>
/// The dependency graph walk, exercised through real path dependencies on disk. Everything here is
/// offline and deterministic: no network, no git, so it behaves identically on all three CI hosts.
/// </summary>
[TestFixture]
public class TestPackageResolver
{
    private NPath WorkDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        WorkDirectory = Path.Combine(Path.GetTempPath(), $"rbt-resolver-{Guid.NewGuid():N}")
            .ToNPath()
            .EnsureDirectoryExists();
    }

    [TearDown]
    public void TearDown()
    {
        WorkDirectory.DeleteIfExists(DeleteMode.Normal);
    }

    /// <summary>Writes a package directory containing a manifest that path-depends on the given names.</summary>
    private NPath WritePackage(string name, params string[] dependsOn)
    {
        var directory = WorkDirectory.Combine(name).EnsureDirectoryExists();
        var entries = dependsOn.Select(dependency => $"\"{dependency}\": {{ \"path\": \"../{dependency}\" }}");
        PackageManifest.PathIn(directory).WriteAllText(
            $"{{ \"name\": \"{name}\", \"dependencies\": {{ {string.Join(", ", entries)} }} }}");
        return directory;
    }

    private PackageRestoreResult Resolve(PackageManifest root, NPath rootDirectory)
    {
        var resolver = new PackageResolver(
            WorkDirectory.Combine("Packages"),
            new IPackageFetcher[] { new PathPackageFetcher() });
        return resolver.Resolve(root, rootDirectory, null, new PackageRestoreOptions(), out _);
    }

    private PackageManifest RootDependingOn(params string[] names)
    {
        var entries = names.Select(name => $"\"{name}\": {{ \"path\": \"../{name}\" }}");
        return PackageManifest.Parse(
            $"{{ \"name\": \"Root\", \"dependencies\": {{ {string.Join(", ", entries)} }} }}",
            WorkDirectory.Combine("root", PackageManifest.FileName));
    }

    [Test]
    public void TransitiveDependenciesAreResolved()
    {
        WritePackage("A", "B");
        WritePackage("B", "C");
        WritePackage("C");
        var rootDirectory = WorkDirectory.Combine("root").EnsureDirectoryExists();

        var result = Resolve(RootDependingOn("A"), rootDirectory);

        // B and C are never named by the root - they are only reachable by reading A's and B's
        // own manifests, which is the whole point of the walk.
        Assert.That(
            result.Packages.Select(package => package.Name).OrderBy(name => name),
            Is.EqualTo(new[] { "A", "B", "C" }));
    }

    [Test]
    public void DiamondDependencyIsFetchedOnce()
    {
        WritePackage("Left", "Shared");
        WritePackage("Right", "Shared");
        WritePackage("Shared");
        var rootDirectory = WorkDirectory.Combine("root").EnsureDirectoryExists();

        var result = Resolve(RootDependingOn("Left", "Right"), rootDirectory);

        Assert.That(result.Packages.Count(package => package.Name == "Shared"), Is.EqualTo(1));
    }

    [Test]
    public void DependencyCycleIsReportedWithTheWholeChain()
    {
        WritePackage("A", "B");
        WritePackage("B", "A");
        var rootDirectory = WorkDirectory.Combine("root").EnsureDirectoryExists();

        var exception = Assert.Throws<PackageException>(() => Resolve(RootDependingOn("A"), rootDirectory));

        // Naming only the package where the walk re-entered would leave the user hunting for the
        // other half of the cycle.
        Assert.That(exception!.Message, Does.Contain("A -> B -> A"));
    }

    [Test]
    public void SelfDependencyIsRejected()
    {
        WritePackage("Solo", "Solo");
        var rootDirectory = WorkDirectory.Combine("root").EnsureDirectoryExists();

        var exception = Assert.Throws<PackageException>(() => Resolve(RootDependingOn("Solo"), rootDirectory));

        Assert.That(exception!.Message, Does.Contain("depends on itself"));
    }

    [Test]
    public void ConflictingPinsAreAHardError()
    {
        // Two spellings of the same package name, pinned to different directories.
        WorkDirectory.Combine("CopyOne").EnsureDirectoryExists();
        WorkDirectory.Combine("CopyTwo").EnsureDirectoryExists();
        var viaA = WorkDirectory.Combine("A").EnsureDirectoryExists();
        PackageManifest.PathIn(viaA).WriteAllText(
            "{ \"dependencies\": { \"Shared\": { \"path\": \"../CopyTwo\" } } }");

        var root = PackageManifest.Parse(
            "{ \"dependencies\": { \"A\": { \"path\": \"../A\" }, " +
            "\"Shared\": { \"path\": \"../CopyOne\" } } }",
            WorkDirectory.Combine("root", PackageManifest.FileName));
        var rootDirectory = WorkDirectory.Combine("root").EnsureDirectoryExists();

        var exception = Assert.Throws<PackageException>(() => Resolve(root, rootDirectory));

        Assert.That(exception!.Message, Does.Contain("conflicting pins"));
        Assert.That(exception.Message, Does.Contain("Shared"));
        // The message has to say how to get unstuck, not just that something is wrong.
        Assert.That(exception.Message, Does.Contain("overrides"));
    }

    [Test]
    public void AnOverrideResolvesAConflict()
    {
        WorkDirectory.Combine("CopyOne").EnsureDirectoryExists();
        WorkDirectory.Combine("CopyTwo").EnsureDirectoryExists();
        var viaA = WorkDirectory.Combine("A").EnsureDirectoryExists();
        PackageManifest.PathIn(viaA).WriteAllText(
            "{ \"dependencies\": { \"Shared\": { \"path\": \"../CopyTwo\" } } }");

        var root = PackageManifest.Parse(
            "{ \"dependencies\": { \"A\": { \"path\": \"../A\" }, " +
            "\"Shared\": { \"path\": \"../CopyOne\" } }, " +
            "\"overrides\": { \"Shared\": { \"path\": \"" + WorkDirectory.Combine("CopyOne").ToString().Replace("\\", "\\\\") + "\" } } }",
            WorkDirectory.Combine("root", PackageManifest.FileName));
        var rootDirectory = WorkDirectory.Combine("root").EnsureDirectoryExists();

        var result = Resolve(root, rootDirectory);

        var shared = result.Packages.Single(package => package.Name == "Shared");
        Assert.That(shared.Root.FileName, Is.EqualTo("CopyOne"));
    }

    /// <summary>
    /// A package fetched from a remote declares its own dependencies, so the names reaching this
    /// walk are no more trustworthy than the manifest they came from - and every one of them
    /// becomes a directory under Packages/.
    /// </summary>
    [TestCase("../outside")]
    [TestCase("..")]
    [TestCase("nested/name")]
    [TestCase("back\\slash")]
    public void APackageNameThatEscapesThePackagesDirectoryIsRejected(string name)
    {
        WorkDirectory.Combine("outside").EnsureDirectoryExists();
        var root = PackageManifest.Parse(
            $"{{ \"dependencies\": {{ \"{name.Replace("\\", "\\\\")}\": {{ \"path\": \"../outside\" }} }} }}",
            WorkDirectory.Combine("root", PackageManifest.FileName));
        var rootDirectory = WorkDirectory.Combine("root").EnsureDirectoryExists();

        var exception = Assert.Throws<PackageException>(() => Resolve(root, rootDirectory));

        Assert.That(exception!.Message, Does.Contain("not a usable package name"));
    }

    [TestCase("Fine")]
    [TestCase("with.dots")]
    [TestCase("with-dash_and_underscore")]
    [TestCase("0leading-digit")]
    public void OrdinaryPackageNamesAreAccepted(string name)
    {
        WritePackage(name);
        var rootDirectory = WorkDirectory.Combine("root").EnsureDirectoryExists();

        var result = Resolve(RootDependingOn(name), rootDirectory);

        Assert.That(result.Packages.Single().Name, Is.EqualTo(name));
    }

    [Test]
    public void MissingPathDependencyNamesWhatItLookedFor()
    {
        var rootDirectory = WorkDirectory.Combine("root").EnsureDirectoryExists();

        var exception = Assert.Throws<PackageException>(
            () => Resolve(RootDependingOn("NotThere"), rootDirectory));

        Assert.That(exception!.Message, Does.Contain("NotThere"));
    }
}
