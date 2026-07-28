using NiceIO;
using ReBuildTool.Service.CompileService;
using ReBuildTool.Service.Context;
using ReBuildTool.Service.PackageService;
using ReBuildTool.ToolChain.Package;
using ResetCore.Common;

namespace ReBuildTool.Test;

/// <summary>
/// Drives a whole project through restore, rule compilation and a real toolchain build, to prove a
/// package's module actually reaches the compiler rather than merely being resolved.
///
/// Everything is generated into a temp directory and uses path dependencies, so it stays offline
/// and deterministic on all three CI hosts.
/// </summary>
[TestFixture]
public class TestPackageBuildIntegration
{
    private NPath WorkDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        WorkDirectory = Path.Combine(Path.GetTempPath(), $"rbt-integration-{Guid.NewGuid():N}")
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

    private NPath CreateConsumingProject(string manifestJson, string dependencyModule)
    {
        var project = WorkDirectory.Combine("Consumer").EnsureDirectoryExists();
        PackageManifest.PathIn(project).WriteAllText(manifestJson);

        var source = project.Combine("Source").EnsureDirectoryExists();
        source.Combine("ConsumerTarget.target.cs").WriteAllText(
            "using ReBuildTool.ToolChain;\n" +
            "public class ConsumerTarget : CppTargetRule\n" +
            "{\n" +
            "    public ConsumerTarget() { UsedModules.Add(\"ConsumerModule\"); }\n" +
            "}\n");

        var module = source.Combine("ConsumerModule").EnsureDirectoryExists();
        module.Combine("ConsumerModule.module.cs").WriteAllText(
            "using ReBuildTool.Service.CompileService;\n" +
            "using ReBuildTool.ToolChain;\n" +
            "public class ConsumerModule : CppModuleRule\n" +
            "{\n" +
            "    public override void Setup(ICppBuildContext buildContext)\n" +
            "    {\n" +
            "        TargetBuildType = BuildType.Executable;\n" +
            $"        Dependencies.Add(\"{dependencyModule}\");\n" +
            "    }\n" +
            "}\n");
        module.Combine("Public").EnsureDirectoryExists().Combine("ConsumerModule.h").WriteAllText(
            "#pragma once\n");
        module.Combine("Private").EnsureDirectoryExists().Combine("ConsumerModule.cpp").WriteAllText(
            "#include \"vendorlib.h\"\n" +
            "#include <cstdio>\n" +
            "int main()\n" +
            "{\n" +
            "    printf(\"%d\\n\", vendor_answer());\n" +
            "    return vendor_answer() == 42 ? 0 : 1;\n" +
            "}\n");
        return project;
    }

    private static void Build(NPath project)
    {
        CmdParser.Parse<TestPackageBuildIntegration>();
        ServiceContext.Instance.Init();
        var cppProject = ServiceContext.Instance.Create<ICppProject>(project).Value;
        cppProject.Parse();
        cppProject.Setup();
        cppProject.Build();
    }

    /// <summary>
    /// Restore has to do exactly that and stop. Routing it through Parse() would also compile and
    /// load the rule assembly and - for a project that has no target yet - scaffold a default
    /// Target/Module, which is not something a cache-warm or offline-prep run should write.
    /// </summary>
    [Test]
    public void RestoreDoesNotCompileRulesOrScaffoldAProject()
    {
        var package = WorkDirectory.Combine("VendorPack").EnsureDirectoryExists();
        PackageManifest.PathIn(package).WriteAllText("{ \"name\": \"VendorPack\" }");

        // Deliberately no Source/ at all: this is the state that would get scaffolded.
        var project = WorkDirectory.Combine("Bare").EnsureDirectoryExists();
        PackageManifest.PathIn(project).WriteAllText(
            "{ \"dependencies\": { \"VendorPack\": { \"path\": \"../VendorPack\" } } }");

        CmdParser.Parse<TestPackageBuildIntegration>();
        ServiceContext.Instance.Init();
        var cppProject = ServiceContext.Instance.Create<ICppProject>(project).Value;
        cppProject.Restore();

        // The packages are there and the lock was written...
        Assert.That(PackageLockFile.ReadFrom(project), Is.Not.Null);
        // ...and nothing else was.
        Assert.That(project.Combine("Source").DirectoryExists(), Is.False,
            "Restore must not scaffold a default project");
        Assert.That(project.Combine("Intermedia").DirectoryExists(), Is.False,
            "Restore must not compile the rule assembly");
    }

    /// <summary>
    /// A header-only prebuilt package: no rule of its own, so rbt synthesizes one, and the include
    /// path it declares has to reach the consuming module's compile line for this to link.
    /// </summary>
    [Test]
    public void ABinaryPackageIsGeneratedIntoTheBuildAndCompiles()
    {
        var package = WorkDirectory.Combine("VendorPack").EnsureDirectoryExists();
        PackageManifest.PathIn(package).WriteAllText(
            "{ \"name\": \"VendorPack\", \"binary\": { \"module\": \"VendorModule\", " +
            "\"includes\": [\"include\"], \"defines\": [], \"artifacts\": [] } }");
        var include = package.Combine("include").EnsureDirectoryExists();
        include.Combine("vendorlib.h").WriteAllText(
            "#pragma once\ninline int vendor_answer() { return 42; }\n");

        var project = CreateConsumingProject(
            "{ \"dependencies\": { \"VendorPack\": { \"path\": \"../VendorPack\" } } }",
            "VendorModule");

        Build(project);

        // The rule rbt generated for the package has to have been compiled into the rule assembly
        // and produced a real library, and the executable must have linked against it.
        var generated = project.Combine(
            "Packages", PackageModuleBinder.GeneratedFolderName, "VendorModule", "VendorModule.module.cs");
        Assert.That(generated.FileExists(), Is.True, "the binary package's module rule should be generated");

        var binaries = project.Combine("Binary").Files(true).Select(file => file.FileName).ToList();
        Assert.That(binaries.Any(name => name.StartsWith("ConsumerModule")), Is.True,
            $"the executable should have been produced, got: {string.Join(", ", binaries)}");
    }

    /// <summary>
    /// An unmodified upstream tree: it ships sources but no rbt rule, and the consuming project
    /// supplies one through <c>overlay</c>.
    /// </summary>
    [Test]
    public void AnOverlayRuleBuildsUpstreamSources()
    {
        var upstream = WorkDirectory.Combine("Upstream").EnsureDirectoryExists();
        upstream.Combine("include").EnsureDirectoryExists().Combine("vendorlib.h").WriteAllText(
            "#pragma once\nint vendor_answer();\n");
        upstream.Combine("src").EnsureDirectoryExists().Combine("vendorlib.cpp").WriteAllText(
            "#include \"vendorlib.h\"\nint vendor_answer() { return 42; }\n");

        var project = CreateConsumingProject(
            "{ \"dependencies\": { \"Upstream\": { \"path\": \"../Upstream\", " +
            "\"overlay\": \"Overlays/VendorModule.module.cs\" } } }",
            "VendorModule");

        // The overlay describes how to build somebody else's source layout - exactly what
        // SourceDirectories and the include paths exist for.
        var overlay = project.Combine("Overlays", "VendorModule.module.cs");
        overlay.EnsureParentDirectoryExists();
        overlay.WriteAllText(
            "using ReBuildTool.Service.CompileService;\n" +
            "using ReBuildTool.ToolChain;\n" +
            "public class VendorModule : CppModuleRule\n" +
            "{\n" +
            "    public override void Setup(ICppBuildContext buildContext)\n" +
            "    {\n" +
            "        TargetBuildType = BuildType.StaticLibrary;\n" +
            "        PublicDefines.Add(\"VENDORMODULE_BUILT_AS_STATIC\");\n" +
            "        PublicIncludePaths.Add(\"include\");\n" +
            "        SourceDirectories.Add(\"src\");\n" +
            "    }\n" +
            "}\n");

        Build(project);

        // The overlay must land in the package root, or its relative "src"/"include" would not resolve.
        Assert.That(upstream.Combine("VendorModule.module.cs").FileExists(), Is.True);
        var binaries = project.Combine("Binary").Files(true).Select(file => file.FileName).ToList();
        Assert.That(binaries.Any(name => name.StartsWith("ConsumerModule")), Is.True,
            $"the executable should have been produced, got: {string.Join(", ", binaries)}");
    }
}
