using NiceIO;
using ReBuildTool.Service.PackageService;
using ReBuildTool.ToolChain;
using ReBuildTool.ToolChain.Package;
using ResetCore.Common;

namespace ReBuildTool.Test;

/// <summary>
/// The two package shapes that carry no rbt rule of their own: a prebuilt binary package, for which
/// rbt synthesizes a module rule, and an unmodified upstream source tree, for which the consuming
/// project supplies one through <c>overlay</c>.
/// </summary>
[TestFixture]
public class TestPackageBinaryModule
{
    private NPath WorkDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        WorkDirectory = Path.Combine(Path.GetTempPath(), $"rbt-binary-{Guid.NewGuid():N}")
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

    private RestoredPackage BinaryPackage(string name, string binaryJson, NPath? overlay = null)
    {
        var root = WorkDirectory.Combine(name).EnsureDirectoryExists();
        var json = $"{{ \"name\": \"{name}\", \"binary\": {binaryJson} }}";
        PackageManifest.PathIn(root).WriteAllText(json);
        return new RestoredPackage(name, root, PackageManifest.Parse(json, root), overlay);
    }

    [Test]
    public void ABinaryPackageGetsAGeneratedModuleRule()
    {
        var package = BinaryPackage("PrebuiltPack",
            "{ \"module\": \"PrebuiltModule\", \"includes\": [\"include\"], \"artifacts\": [] }");
        var packagesRoot = WorkDirectory.Combine("Packages");

        var roots = PackageModuleBinder.Bind(packagesRoot, new[] { package });

        var generated = packagesRoot.Combine(
            PackageModuleBinder.GeneratedFolderName, "PrebuiltModule", "PrebuiltModule.module.cs");
        Assert.That(generated.FileExists(), Is.True);
        Assert.That(roots.Any(root => root == generated.Parent), Is.True,
            "the generated rule directory must be handed back as a glob root");
        Assert.That(generated.ReadAllText(), Does.Contain("class PrebuiltModule : CppModuleRule"));
        // The framework registers a module's Public/Private dirs unconditionally; they must exist
        // or every build logs a missing-path warning for them.
        Assert.That(generated.Parent.Combine("Public").DirectoryExists(), Is.True);
        Assert.That(generated.Parent.Combine("Private").DirectoryExists(), Is.True);
    }

    [Test]
    public void TheModuleNameDefaultsToThePackageName()
    {
        var package = BinaryPackage("SoloPack", "{ \"artifacts\": [] }");
        var packagesRoot = WorkDirectory.Combine("Packages");

        PackageModuleBinder.Bind(packagesRoot, new[] { package });

        Assert.That(
            packagesRoot.Combine(PackageModuleBinder.GeneratedFolderName, "SoloPack", "SoloPack.module.cs")
                .FileExists(),
            Is.True);
    }

    /// <summary>
    /// The generated file must not encode the platform being built: NeedReBuildRuleAssembly
    /// compares timestamps, so a file that changed content per --TargetPlatform would recompile
    /// every rule on each switch.
    /// </summary>
    [Test]
    public void TheGeneratedRuleIsPlatformIndependentAndDoesNotChurn()
    {
        var package = BinaryPackage("PrebuiltPack",
            "{ \"artifacts\": [ { \"platform\": \"Windows\", \"arch\": \"x64\", " +
            "\"staticLibraries\": [\"some.lib\"] } ] }");
        var packagesRoot = WorkDirectory.Combine("Packages");
        PackageModuleBinder.Bind(packagesRoot, new[] { package });
        var generated = packagesRoot.Combine(
            PackageModuleBinder.GeneratedFolderName, "PrebuiltPack", "PrebuiltPack.module.cs");
        var writtenAt = File.GetLastWriteTimeUtc(generated);
        Assert.That(generated.ReadAllText(), Does.Not.Contain("some.lib"));

        Thread.Sleep(1100);
        PackageModuleBinder.Bind(packagesRoot, new[] { package });

        Assert.That(File.GetLastWriteTimeUtc(generated), Is.EqualTo(writtenAt));
    }

    /// <summary>
    /// The module name is interpolated into a directory name and into the "public class &lt;name&gt;"
    /// of a rule that rbt compiles and executes. A remote package's manifest is not the consuming
    /// project's to trust, so anything but a plain identifier has to be refused - escaping it would
    /// still leave a package able to name a class it has no business naming.
    /// </summary>
    [TestCase("../escape")]
    [TestCase("Evil { } public class Injected : CppModuleRule { //")]
    [TestCase("has space")]
    [TestCase("1StartsWithDigit")]
    public void AModuleNameThatIsNotAPlainIdentifierIsRejected(string moduleName)
    {
        var package = BinaryPackage("PrebuiltPack",
            $"{{ \"module\": \"{moduleName.Replace("\\", "\\\\").Replace("\"", "\\\"")}\", \"artifacts\": [] }}");
        var packagesRoot = WorkDirectory.Combine("Packages");

        var exception = Assert.Throws<PackageException>(
            () => PackageModuleBinder.Bind(packagesRoot, new[] { package }));

        Assert.That(exception!.Message, Does.Contain("PrebuiltPack"));
        Assert.That(exception.Message, Does.Contain("identifier"));
    }

    /// <summary>
    /// Both packages would generate to Packages/.generated/&lt;module&gt;/&lt;module&gt;.module.cs - one file.
    /// Left alone the second write wins and the build quietly depends on processing order, so the
    /// collision has to be reported instead. (The same clash between two source packages is caught
    /// later in ParseRules, where they are two distinct files claiming one name.)
    /// </summary>
    [Test]
    public void TwoBinaryPackagesClaimingOneModuleNameIsAnError()
    {
        var first = BinaryPackage("FirstPack", "{ \"module\": \"SharedName\", \"artifacts\": [] }");
        var second = BinaryPackage("SecondPack", "{ \"module\": \"SharedName\", \"artifacts\": [] }");

        var exception = Assert.Throws<Exception>(
            () => PackageModuleBinder.Bind(WorkDirectory.Combine("Packages"), new[] { first, second }));

        // Both culprits have to be named, or the user has no idea which two packages to look at.
        Assert.That(exception!.Message, Does.Contain("FirstPack"));
        Assert.That(exception.Message, Does.Contain("SecondPack"));
        Assert.That(exception.Message, Does.Contain("SharedName"));
    }

    [Test]
    public void DistinctModuleNamesFromSeveralPackagesCoexist()
    {
        var first = BinaryPackage("FirstPack", "{ \"module\": \"FirstModule\", \"artifacts\": [] }");
        var second = BinaryPackage("SecondPack", "{ \"module\": \"SecondModule\", \"artifacts\": [] }");

        var roots = PackageModuleBinder.Bind(WorkDirectory.Combine("Packages"), new[] { first, second });

        Assert.That(roots, Has.Count.EqualTo(2));
    }

    [Test]
    public void AnOverlayIsCopiedIntoThePackageItDescribes()
    {
        var overlay = WorkDirectory.Combine("Overlays", "upstream.module.cs");
        overlay.EnsureParentDirectoryExists();
        overlay.WriteAllText("public class upstream { }");
        var root = WorkDirectory.Combine("UpstreamPack").EnsureDirectoryExists();
        var package = new RestoredPackage("UpstreamPack", root, null, overlay);

        PackageModuleBinder.Bind(WorkDirectory.Combine("Packages"), new[] { package });

        // Into the package root, so the overlay's relative SourceDirectories/ExcludeFiles resolve
        // against the upstream tree rather than against some generated directory.
        Assert.That(root.Combine("upstream.module.cs").FileExists(), Is.True);
    }

    private static ICppBuildContext BuildContext()
    {
        CmdParser.Parse<TestPackageBinaryModule>();
        return new CppBuilder();
    }

    [Test]
    public void ArtifactsAreSelectedByPlatformArchAndConfig()
    {
        var context = BuildContext();
        var platform = IPlatformSupport.CurrentTargetPlatform.ToString();
        var arch = context.CurrentBuildOption.Architecture.CommandLineName;
        var config = context.CurrentBuildOption.Configuration.ToString();

        var root = WorkDirectory.Combine("Pack").EnsureDirectoryExists();
        PackageManifest.PathIn(root).WriteAllText(
            "{ \"name\": \"Pack\", \"binary\": { \"includes\": [\"include\"], \"artifacts\": [" +
            $"{{ \"platform\": \"{platform}\", \"arch\": \"{arch}\", \"config\": \"{config}\", " +
            "\"libraryDirectories\": [\"lib\"], \"staticLibraries\": [\"wanted\"] }, " +
            "{ \"platform\": \"NotAPlatform\", \"staticLibraries\": [\"unwanted\"] } ] } }");

        var module = new SyntheticModule { ModuleDirectoryForTest = root.ToString() };
        PackageArtifactSelector.Apply(module, context, PackageManifest.PathIn(root).ToString());

        Assert.That(module.PublicStaticLibraries, Is.EqualTo(new[] { "wanted" }));
        // Include and library directories are package-relative and must come back absolute.
        Assert.That(module.PublicIncludePaths.Single(), Is.EqualTo(root.Combine("include").ToString()));
        Assert.That(module.PublicLibraryDirectories.Single(), Is.EqualTo(root.Combine("lib").ToString()));
    }

    /// <summary>
    /// A relative path is the package describing its own layout. Letting it climb out would put
    /// arbitrary directories of the consuming machine on the include or library search path.
    /// </summary>
    [TestCase("\\\"includes\\\": [\\\"../../elsewhere\\\"], \\\"artifacts\\\": []")]
    [TestCase("\\\"artifacts\\\": [ { \\\"libraryDirectories\\\": [\\\"../../elsewhere\\\"] } ]")]
    public void ARelativePathEscapingThePackageIsRejected(string binaryBody)
    {
        var context = BuildContext();
        var root = WorkDirectory.Combine("Pack").EnsureDirectoryExists();
        PackageManifest.PathIn(root).WriteAllText(
            "{ \"name\": \"Pack\", \"binary\": { " + binaryBody.Replace("\\\"", "\"") + " } }");

        var module = new SyntheticModule { ModuleDirectoryForTest = root.ToString() };
        var exception = Assert.Throws<PackageException>(
            () => PackageArtifactSelector.Apply(module, context, PackageManifest.PathIn(root).ToString()));

        Assert.That(exception!.Message, Does.Contain("outside the package"));
    }

    /// <summary>
    /// Absolute entries stay allowed: that is exactly what the vcpkg bridge emits, because a vcpkg
    /// installed tree lives outside Packages/ by design.
    /// </summary>
    [Test]
    public void AnAbsolutePathIsPassedThrough()
    {
        var context = BuildContext();
        var elsewhere = WorkDirectory.Combine("vcpkg-ish").EnsureDirectoryExists();
        var root = WorkDirectory.Combine("Pack").EnsureDirectoryExists();
        PackageManifest.PathIn(root).WriteAllText(
            "{ \"name\": \"Pack\", \"binary\": { \"includes\": [\"" +
            elsewhere.ToString().Replace("\\", "\\\\") + "\"], \"artifacts\": [] } }");

        var module = new SyntheticModule { ModuleDirectoryForTest = root.ToString() };
        PackageArtifactSelector.Apply(module, context, PackageManifest.PathIn(root).ToString());

        Assert.That(module.PublicIncludePaths.Single(), Is.EqualTo(elsewhere.ToString()));
    }

    [Test]
    public void AnArtifactWithoutSelectorsMatchesEveryPlatform()
    {
        var context = BuildContext();
        var root = WorkDirectory.Combine("Pack").EnsureDirectoryExists();
        PackageManifest.PathIn(root).WriteAllText(
            "{ \"name\": \"Pack\", \"binary\": { \"artifacts\": [ " +
            "{ \"staticLibraries\": [\"everywhere\"] } ] } }");

        var module = new SyntheticModule { ModuleDirectoryForTest = root.ToString() };
        PackageArtifactSelector.Apply(module, context, PackageManifest.PathIn(root).ToString());

        Assert.That(module.PublicStaticLibraries, Is.EqualTo(new[] { "everywhere" }));
    }

    /// <summary>Stands in for the rule rbt generates, so the selector can be tested on its own.</summary>
    private class SyntheticModule : CppModuleRule
    {
        public string ModuleDirectoryForTest
        {
            set => ModuleDirectory = value;
        }
    }
}
