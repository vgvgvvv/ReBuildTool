using NiceIO;
using ReBuildTool.Service.CompileService;
using ReBuildTool.Service.Context;
using ReBuildTool.Service.IDEService;
using ReBuildTool.ToolChain.Project;
using ResetCore.Common;

namespace ReBuildTool.Test;

/// <summary>
/// Visual Studio filter generation for modules that come from packages.
///
/// The filter walk climbs from a module directory up to the project's <c>Source</c> folder. Only a
/// module living under <c>Source/</c> ever reaches that sentinel - a package's module sits under
/// <c>Packages/</c>, and a package consumed through a path dependency is not under the project at
/// all - so without a stop at the project root the walk ran off the top of the tree and threw
/// "not valid on a root level directory".
///
/// This only ever fired on Windows, because every other host defaults to the CMake generator.
/// </summary>
[TestFixture]
public class TestPackageVsFilters
{
    private NPath WorkDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        WorkDirectory = Path.Combine(Path.GetTempPath(), $"rbt-vsfilter-{Guid.NewGuid():N}")
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

    [Test]
    public void APackageOutsideTheProjectDoesNotBreakFilterGeneration()
    {
        // The package lives beside the project, not inside it - a path dependency, the shape that
        // has no ancestor in common with the project below the drive root.
        var package = WorkDirectory.Combine("VendorPackage").EnsureDirectoryExists();
        PackageManifestFor(package);
        package.Combine("VendorModule.module.cs").WriteAllText(
            "using ReBuildTool.Service.CompileService;\n" +
            "using ReBuildTool.ToolChain;\n" +
            "public class VendorModule : CppModuleRule\n" +
            "{\n" +
            "    public override void Setup(ICppBuildContext buildContext)\n" +
            "    {\n" +
            "        TargetBuildType = BuildType.StaticLibrary;\n" +
            "        PublicDefines.Add(\"VENDORMODULE_BUILT_AS_STATIC\");\n" +
            "    }\n" +
            "}\n");
        package.Combine("Public").EnsureDirectoryExists().Combine("VendorModule.h").WriteAllText(
            "#pragma once\nint vendor_answer();\n");
        package.Combine("Private").EnsureDirectoryExists().Combine("VendorModule.cpp").WriteAllText(
            "#include \"VendorModule.h\"\nint vendor_answer() { return 42; }\n");

        var project = WorkDirectory.Combine("Consumer").EnsureDirectoryExists();
        project.Combine("RBTPackage.json").WriteAllText(
            "{ \"dependencies\": { \"VendorPackage\": { \"path\": \"../VendorPackage\" } } }");
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
            "        Dependencies.Add(\"VendorModule\");\n" +
            "    }\n" +
            "}\n");
        module.Combine("Public").EnsureDirectoryExists().Combine("ConsumerModule.h").WriteAllText(
            "#pragma once\n");
        module.Combine("Private").EnsureDirectoryExists().Combine("ConsumerModule.cpp").WriteAllText(
            "#include \"VendorModule.h\"\nint main() { return vendor_answer() == 42 ? 0 : 1; }\n");

        CmdParser.Parse<TestPackageVsFilters>();
        ServiceContext.Instance.Init();
        ProjectGenArgs.Get().IDEProjectType.Value = ProjectGenType.VisualStudio;

        var cppProject = ServiceContext.Instance.Create<ICppProject>(project).Value;
        cppProject.Parse();
        Assert.DoesNotThrow(() => cppProject.Setup());

        var filters = project.Combine("Intermedia/CppProject/VCProjects")
            .Files("*.vcxproj.filters", true)
            .ToList();
        Assert.That(filters, Is.Not.Empty, "a .vcxproj.filters should have been generated");

        var text = filters.First().ReadAllText();
        // The out-of-project package is grouped under Modules/ rather than named with a "../../"
        // filter, which is not something Solution Explorer can display. (File Include paths are
        // relative to the output folder and legitimately contain "..", so only the Filter
        // declarations are checked here.)
        Assert.That(text.Replace('\\', '/'), Does.Contain("Modules/VendorPackage"));

        var filterNames = System.Text.RegularExpressions.Regex
            .Matches(text, "<Filter Include=\"([^\"]*)\"")
            .Select(match => match.Groups[1].Value)
            .ToList();
        Assert.That(filterNames, Is.Not.Empty);
        Assert.That(filterNames.Any(name => name.Replace('\\', '/').StartsWith("Modules/VendorPackage")),
            Is.True, $"expected a Modules/VendorPackage filter, got: {string.Join(", ", filterNames)}");
        Assert.That(filterNames.Any(name => name.Contains("..")), Is.False,
            $"filter names must not climb out of the project, got: {string.Join(", ", filterNames)}");
    }

    private static void PackageManifestFor(NPath package)
    {
        package.Combine("RBTPackage.json").WriteAllText("{ \"name\": \"VendorPackage\" }");
    }
}
