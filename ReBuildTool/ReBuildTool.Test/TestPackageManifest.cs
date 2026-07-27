using NiceIO;
using ReBuildTool.Service.PackageService;

namespace ReBuildTool.Test;

/// <summary>
/// Manifest validation and lock-file behaviour. A bad manifest has to fail with a message that
/// says what to write instead - these are the errors a user meets first.
/// </summary>
[TestFixture]
public class TestPackageManifest
{
    private NPath WorkDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        WorkDirectory = Path.Combine(Path.GetTempPath(), $"rbt-manifest-{Guid.NewGuid():N}")
            .ToNPath()
            .EnsureDirectoryExists();
    }

    [TearDown]
    public void TearDown()
    {
        WorkDirectory.DeleteIfExists(DeleteMode.Normal);
    }

    private static PackageDependency DependencyFrom(string json)
    {
        return PackageManifest.Parse($"{{ \"dependencies\": {{ \"Some\": {json} }} }}", "test".ToNPath())
            .Dependencies["Some"];
    }

    [Test]
    public void ADependencyWithNoSourceIsRejected()
    {
        var exception = Assert.Throws<PackageException>(() => DependencyFrom("{ }").ResolveKind("Some"));

        Assert.That(exception!.Message, Does.Contain("git"));
        Assert.That(exception.Message, Does.Contain("path"));
    }

    [Test]
    public void ADependencyWithTwoSourcesIsRejected()
    {
        var dependency = DependencyFrom("{ \"git\": \"https://x/y.git\", \"tag\": \"v1\", \"path\": \"../y\" }");

        var exception = Assert.Throws<PackageException>(() => dependency.ResolveKind("Some"));

        Assert.That(exception!.Message, Does.Contain("more than one source"));
    }

    [Test]
    public void AGitDependencyWithoutARevisionIsRejected()
    {
        // Without a pin the build is not reproducible, and rbt deliberately has no solver to
        // choose one - so this must fail loudly rather than silently take the default branch.
        var exception = Assert.Throws<PackageException>(
            () => DependencyFrom("{ \"git\": \"https://x/y.git\" }").ResolveKind("Some"));

        Assert.That(exception!.Message, Does.Contain("commit"));
        Assert.That(exception.Message, Does.Contain("tag"));
    }

    [Test]
    public void GitRevisionPrefersTheMostSpecificPin()
    {
        var dependency = DependencyFrom(
            "{ \"git\": \"https://x/y.git\", \"commit\": \"abc123\", \"tag\": \"v1\", \"branch\": \"main\" }");

        Assert.That(dependency.GitRevision, Is.EqualTo("abc123"));
        Assert.That(DependencyFrom("{ \"git\": \"https://x/y.git\", \"tag\": \"v1\", \"branch\": \"main\" }").GitRevision,
            Is.EqualTo("v1"));
    }

    [Test]
    public void PinsDifferWhenTheRevisionDiffers()
    {
        var one = DependencyFrom("{ \"git\": \"https://x/y.git\", \"tag\": \"v1\" }");
        var two = DependencyFrom("{ \"git\": \"https://x/y.git\", \"tag\": \"v2\" }");
        var same = DependencyFrom("{ \"git\": \"https://x/y.git\", \"tag\": \"v1\" }");

        Assert.That(one.PinKey("Some"), Is.Not.EqualTo(two.PinKey("Some")));
        Assert.That(one.PinKey("Some"), Is.EqualTo(same.PinKey("Some")));
    }

    [Test]
    public void AMissingManifestIsNotAnError()
    {
        // A package is allowed to ship no manifest at all - it simply has no dependencies.
        Assert.That(PackageManifest.ReadFrom(WorkDirectory), Is.Null);
    }

    [Test]
    public void InvalidJsonNamesTheOffendingFile()
    {
        var path = PackageManifest.PathIn(WorkDirectory);
        path.WriteAllText("{ this is not json");

        var exception = Assert.Throws<PackageException>(() => PackageManifest.ReadFrom(WorkDirectory));

        Assert.That(exception!.Message, Does.Contain(PackageManifest.FileName));
    }

    [Test]
    public void ANullDependencyMapDeserializesToAnEmptyOne()
    {
        var manifest = PackageManifest.Parse("{ \"dependencies\": null }", "test".ToNPath());

        Assert.That(manifest.Dependencies, Is.Not.Null);
        Assert.That(manifest.Dependencies, Is.Empty);
    }

    [Test]
    public void TheLockRoundTrips()
    {
        var original = new PackageLockFile
        {
            Packages =
            {
                new LockedPackage
                {
                    Name = "Some",
                    Source = "Git",
                    Origin = "https://x/y.git",
                    Resolved = "abc123",
                    Pin = "git:https://x/y.git@v1",
                    Dependencies = { "Other" }
                }
            }
        };
        original.WriteIfChanged(WorkDirectory);

        var reread = PackageLockFile.ReadFrom(WorkDirectory);

        Assert.That(reread, Is.Not.Null);
        var package = reread!.Find("Some");
        Assert.That(package, Is.Not.Null);
        Assert.That(package!.Resolved, Is.EqualTo("abc123"));
        Assert.That(package.Pin, Is.EqualTo("git:https://x/y.git@v1"));
        Assert.That(package.Dependencies, Is.EqualTo(new[] { "Other" }));
    }

    /// <summary>
    /// An unchanged lock must not be rewritten. rbt's incremental checks are timestamp based
    /// (NeedReBuildRuleAssembly, the makefile backend), so a needless rewrite on every build would
    /// keep re-triggering work downstream.
    /// </summary>
    [Test]
    public void RewritingAnUnchangedLockDoesNotTouchTheFile()
    {
        var lockFile = new PackageLockFile
        {
            Packages = { new LockedPackage { Name = "Some", Source = "Git", Resolved = "abc123" } }
        };
        lockFile.WriteIfChanged(WorkDirectory);
        var path = PackageLockFile.PathIn(WorkDirectory);
        var writtenAt = File.GetLastWriteTimeUtc(path);

        // Coarse filesystem timestamps would hide a rewrite that happened within the same tick.
        Thread.Sleep(1100);
        PackageLockFile.ReadFrom(WorkDirectory)!.WriteIfChanged(WorkDirectory);

        Assert.That(File.GetLastWriteTimeUtc(path), Is.EqualTo(writtenAt));
    }

    [Test]
    public void ALockFromAFutureVersionIsIgnoredRatherThanFatal()
    {
        PackageLockFile.PathIn(WorkDirectory).WriteAllText("{ \"version\": 999, \"packages\": [] }");

        // Re-resolving is always correct, so an unreadable lock must not break the build.
        Assert.That(PackageLockFile.ReadFrom(WorkDirectory), Is.Null);
    }
}
