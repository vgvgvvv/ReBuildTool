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
