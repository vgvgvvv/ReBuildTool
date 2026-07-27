using NiceIO;
using ReBuildTool.Service.PackageService;

namespace ReBuildTool.Test;

/// <summary>
/// <c>--PackageAdd</c> / <c>--PackageRemove</c>: the spec grammar and the manifest edits.
/// </summary>
[TestFixture]
public class TestPackageManifestEditor
{
    private NPath WorkDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        WorkDirectory = Path.Combine(Path.GetTempPath(), $"rbt-editor-{Guid.NewGuid():N}")
            .ToNPath()
            .EnsureDirectoryExists();
    }

    [TearDown]
    public void TearDown()
    {
        WorkDirectory.DeleteIfExists(DeleteMode.Normal);
    }

    private PackageManifest Manifest()
    {
        return PackageManifest.ReadFrom(WorkDirectory)!;
    }

    [Test]
    public void AGitSpecWithATagIsParsed()
    {
        var dependency = PackageManifestEditor.ParseSpec("git:https://github.com/x/y.git#v1.2.0");

        Assert.That(dependency.Git, Is.EqualTo("https://github.com/x/y.git"));
        Assert.That(dependency.Tag, Is.EqualTo("v1.2.0"));
        Assert.That(dependency.Commit, Is.Null);
    }

    /// <summary>A full sha is recorded as a commit, not as a tag, so the manifest says what it means.</summary>
    [Test]
    public void AFortyCharacterHexQualifierIsTreatedAsACommit()
    {
        var sha = new string('a', 40);

        var dependency = PackageManifestEditor.ParseSpec($"git:https://github.com/x/y.git#{sha}");

        Assert.That(dependency.Commit, Is.EqualTo(sha));
        Assert.That(dependency.Tag, Is.Null);
    }

    /// <summary>The qualifier is split off the last '#', because a URL may contain one itself.</summary>
    [Test]
    public void AUrlContainingAHashIsSplitOnTheLastOne()
    {
        var dependency = PackageManifestEditor.ParseSpec("url:https://host/a#b/pkg.zip#abc123");

        Assert.That(dependency.Url, Is.EqualTo("https://host/a#b/pkg.zip"));
        Assert.That(dependency.Sha256, Is.EqualTo("abc123"));
    }

    [Test]
    public void PathAndVcpkgSpecsAreParsed()
    {
        Assert.That(PackageManifestEditor.ParseSpec("path:../Local").Path, Is.EqualTo("../Local"));

        var vcpkg = PackageManifestEditor.ParseSpec("vcpkg:fmt#x64-windows");
        Assert.That(vcpkg.Vcpkg, Is.EqualTo("fmt"));
        Assert.That(vcpkg.Triplet, Is.EqualTo("x64-windows"));
    }

    [Test]
    public void AGitSpecWithoutARevisionIsRejected()
    {
        var exception = Assert.Throws<PackageException>(
            () => PackageManifestEditor.ParseSpec("git:https://github.com/x/y.git"));

        Assert.That(exception!.Message, Does.Contain("exact pins"));
    }

    [Test]
    public void AnUnknownSourceIsRejected()
    {
        var exception = Assert.Throws<PackageException>(
            () => PackageManifestEditor.ParseSpec("svn://somewhere"));

        Assert.That(exception!.Message, Does.Contain("svn"));
    }

    [Test]
    public void AddCreatesTheManifestWhenThereIsNone()
    {
        PackageManifestEditor.Add(WorkDirectory, "MyLib=git:https://github.com/x/y.git#v1.0");

        var dependency = Manifest().Dependencies["MyLib"];
        Assert.That(dependency.Git, Is.EqualTo("https://github.com/x/y.git"));
        Assert.That(dependency.Tag, Is.EqualTo("v1.0"));
    }

    /// <summary>
    /// The edit goes through the raw JSON so fields rbt does not model survive it - a manifest is
    /// the user's file, not rbt's serialization format.
    /// </summary>
    [Test]
    public void AddPreservesFieldsItDoesNotUnderstand()
    {
        PackageManifest.PathIn(WorkDirectory).WriteAllText(
            "{ \"name\": \"Mine\", \"somethingElse\": { \"keep\": true }, " +
            "\"dependencies\": { \"Existing\": { \"path\": \"../Existing\" } } }");

        PackageManifestEditor.Add(WorkDirectory, "MyLib=path:../MyLib");

        var text = PackageManifest.PathIn(WorkDirectory).ReadAllText();
        Assert.That(text, Does.Contain("somethingElse"));
        Assert.That(text, Does.Contain("keep"));
        Assert.That(Manifest().Dependencies.Keys, Is.EquivalentTo(new[] { "Existing", "MyLib" }));
    }

    /// <summary>
    /// A PackageDependency carries one field per source kind; writing the empty ones out would bury
    /// the entry that matters in a wall of nulls.
    /// </summary>
    [Test]
    public void AddDoesNotWriteEmptyFields()
    {
        PackageManifestEditor.Add(WorkDirectory, "MyLib=path:../MyLib");

        var text = PackageManifest.PathIn(WorkDirectory).ReadAllText();
        Assert.That(text, Does.Not.Contain("null"));
        Assert.That(text, Does.Not.Contain("\"git\""));
        Assert.That(text, Does.Not.Contain("\"strip\""));
    }

    [Test]
    public void AddReplacesAnExistingEntry()
    {
        PackageManifestEditor.Add(WorkDirectory, "MyLib=git:https://github.com/x/y.git#v1.0");
        PackageManifestEditor.Add(WorkDirectory, "MyLib=git:https://github.com/x/y.git#v2.0");

        Assert.That(Manifest().Dependencies["MyLib"].Tag, Is.EqualTo("v2.0"));
    }

    [Test]
    public void RemoveDropsTheEntry()
    {
        PackageManifestEditor.Add(WorkDirectory, "MyLib=path:../MyLib");
        PackageManifestEditor.Add(WorkDirectory, "Other=path:../Other");

        Assert.That(PackageManifestEditor.Remove(WorkDirectory, "MyLib"), Is.True);
        Assert.That(Manifest().Dependencies.Keys, Is.EquivalentTo(new[] { "Other" }));
    }

    [Test]
    public void RemovingSomethingAbsentIsNotAnError()
    {
        PackageManifestEditor.Add(WorkDirectory, "MyLib=path:../MyLib");

        Assert.That(PackageManifestEditor.Remove(WorkDirectory, "NotThere"), Is.False);
        Assert.That(Manifest().Dependencies.Keys, Is.EquivalentTo(new[] { "MyLib" }));
    }

    [Test]
    public void AMalformedAddIsRejectedWithAnExample()
    {
        var exception = Assert.Throws<PackageException>(
            () => PackageManifestEditor.Add(WorkDirectory, "no-equals-sign"));

        Assert.That(exception!.Message, Does.Contain("<Name>=<source spec>"));
    }
}
