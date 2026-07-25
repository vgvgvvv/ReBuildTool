using System.Reflection;
using System.Runtime.InteropServices;
using NiceIO;
using ReBuildTool.Service.CompileService;
using ReBuildTool.Service.Context;
using ReBuildTool.Service.IDEService;
using ReBuildTool.ToolChain;
using ReBuildTool.ToolChain.Project;
using ResetCore.Common;

namespace ReBuildTool.Test;

public class Tests
{

    private NPath BuildCppFolder;

    [SetUp]
    public void Setup()
    {

        BuildCppFolder = TestCaseGlobalVars.SampleDirectory.Combine("BuildCpp");
    }

    // One subdirectory under Sample/ per compile scenario: plain executable (BuildCpp),
    // static-library linking, dynamic-library linking, and a three-level module chain.
    [TestCase("BuildCpp")]
    [TestCase("StaticLibraryLink")]
    [TestCase("DynamicLibraryLink")]
    [TestCase("MultiModuleChain")]
    public void TestSampleProjectBuild(string sampleName)
    {
        CmdParser.Parse<Tests>();
        ServiceContext.Instance.Init();

        var path = TestCaseGlobalVars.SampleDirectory.Combine(sampleName);
        var project = ServiceContext.Instance.Create<ICppProject>(path).Value;
        project.Parse();
        project.Setup();
        project.Build();
    }

    [Test]
    public void AllReferencedAssemblies()
    {
        var entryAssembly = Assembly.GetAssembly(typeof(Tests));
        Log.Info(entryAssembly.FullName);
        foreach (var assemblyPath in entryAssembly.Location.ToNPath().Parent.Files("*.dll", true))
        {
            if (!MonoUtil.IsDotNetAssembly(assemblyPath))
            {
                continue;
            }
            var assembly = Assembly.LoadFrom(assemblyPath);
            Log.Info(assembly.FullName);
        }
    }

    [Test]
    public void GetRuntimePath()
    {
        string frameworkPath = RuntimeEnvironment.GetRuntimeDirectory();
        Log.Info(frameworkPath);
    }

    [Test]
    public void TestBaseProject()
    {
        CmdParser.Parse<Tests>();
        ServiceContext.Instance.Init();
        //CppCompilerArgs.Get().DryRun = true;
        
        var path = BuildCppFolder;
        var project = ServiceContext.Instance.Create<ICppProject>(path).Value;
        project.Parse();
        project.Setup();
        project.Build();
        
    }

    [Test]
    public void TestProjectGenerate()
    {
        CmdParser.Parse<Tests>();
        ServiceContext.Instance.Init();
        //CppCompilerArgs.Get().DryRun = true;

        var path = BuildCppFolder;
        var project = ServiceContext.Instance.Create<ICppProject>(path).Value;
        project.Parse();
        project.Setup();
    }

    // Generates the IDE project for a sample containing an Executable module and verifies a
    // companion launcher (.vcxproj + .vcxproj.user) is emitted alongside the host VCProject.
    [Test]
    public void TestLauncherProjectGenerate()
    {
        CmdParser.Parse<Tests>();
        ServiceContext.Instance.Init();

        var path = TestCaseGlobalVars.SampleDirectory.Combine("StaticLibraryLink");
        var project = ServiceContext.Instance.Create<ICppProject>(path).Value;
        project.Parse();
        project.Setup();

        var vcProjectsDir = path.Combine("Intermedia/CppProject/VCProjects");
        Assert.IsTrue(vcProjectsDir.Exists(), "VCProjects output dir should exist");

        // Host Makefile project must still be generated.
        Assert.IsTrue(vcProjectsDir.Combine("StaticLibraryLink.vcxproj").FileExists(),
            "host vcxproj should be generated");

        // Launcher for the AppModule executable module (name = {host}_{exe}).
        var launcherVcxproj = vcProjectsDir.Combine("StaticLibraryLink_AppModule.vcxproj");
        var launcherUser = vcProjectsDir.Combine("StaticLibraryLink_AppModule.vcxproj.user");
        Assert.IsTrue(launcherVcxproj.FileExists(),
            "launcher vcxproj should be generated for the executable module");
        Assert.IsTrue(launcherUser.FileExists(),
            "launcher .vcxproj.user should be generated for the executable module");

        // The launcher's .vcxproj.user must point the debugger at the exe output.
        var userContent = launcherUser.ReadAllLines();
        Assert.IsTrue(userContent.Any(l => l.Contains("LocalDebuggerCommand")),
            ".vcxproj.user must contain LocalDebuggerCommand");
        Assert.IsTrue(userContent.Any(l => l.Contains("DebuggerFlavor")),
            ".vcxproj.user must contain DebuggerFlavor");
    }


    // Generates the CMake project for a sample and verifies the rbt-driven build wiring:
    //  - the real per-module targets are emitted EXCLUDE_FROM_ALL (IntelliSense only),
    //  - a single root add_custom_target(... ALL ...) drives the actual build through rbt,
    //  - compile_commands.json export is on (what the IDE reads for hints/navigation),
    //  - no rbt call is emitted at CMake configure time (execute_process removed).
    [Test]
    public void TestCMakeProjectGenerate()
    {
        CmdParser.Parse<Tests>();
        ServiceContext.Instance.Init();

        // On Windows the default IDE project is VS - force CMake generation for this test.
        ProjectGenArgs.Get().IDEProjectType.Value = ProjectGenType.CMake;

        var path = TestCaseGlobalVars.SampleDirectory.Combine("StaticLibraryLink");
        var project = ServiceContext.Instance.Create<ICppProject>(path).Value;
        project.Parse();
        project.Setup();

        // Root CMakeLists is written to the project root.
        var rootCMake = path.Combine("CMakeLists.txt");
        Assert.IsTrue(rootCMake.Exists(), "root CMakeLists.txt should be generated");

        var rootText = rootCMake.ReadAllText();
        Assert.IsTrue(rootText.Contains("set(CMAKE_EXPORT_COMPILE_COMMANDS ON)"),
            "root must enable compile_commands.json for IDE IntelliSense");
        Assert.IsTrue(rootText.Contains("add_custom_target(StaticLibraryLink_rbt_build ALL"),
            "root must drive the real build through an rbt custom target on ALL");
        Assert.IsTrue(rootText.Contains("--Mode Build"),
            "the rbt custom target must invoke an rbt build");
        Assert.IsFalse(rootText.Contains("execute_process"),
            "no rbt call should run at CMake configure time");

        // The IDE's config selector drives rbt's --BuildConfig via $<CONFIG>, and rbt's configs
        // are declared as the CMake configurations so Debug/Release/... map cleanly.
        Assert.IsTrue(rootText.Contains("--BuildConfig \"$<CONFIG>\""),
            "the rbt build config must follow the CMake $<CONFIG> selection");
        Assert.IsTrue(rootText.Contains("Debug;Release;ReleasePlus;ReleaseSize"),
            "root must declare rbt's build configurations for the IDE config selector");

        // Every module's real target is emitted for IntelliSense only (kept out of ALL).
        var cmakeProjectsDir = path.Combine("Intermedia/CppProject/CMakeProjects");
        var moduleCMakeFiles = cmakeProjectsDir.Files("CMakeLists.txt", true).ToList();
        Assert.IsNotEmpty(moduleCMakeFiles, "module CMakeLists files should be generated");
        foreach (var moduleCMake in moduleCMakeFiles)
        {
            Assert.IsTrue(moduleCMake.ReadAllText().Contains("EXCLUDE_FROM_ALL TRUE"),
                $"module target in {moduleCMake} must be EXCLUDE_FROM_ALL (native build off)");
        }
    }

    // End-to-end HeaderTool (RHT) codegen: a module with a RECLASS-annotated
    // header is built with the ResetEngineClassExtension plugin. Verifies the
    // full chain — HeaderTool runs, generates HeaderToolGen/, the framework
    // auto-injects those paths, and the generated .ext.gen.cpp compiles/links.
    // Asserts the expected generated files exist after a successful build.
    [Test]
    public void TestHeaderToolCodegen()
    {
        CmdParser.Parse<Tests>();
        ServiceContext.Instance.Init();

        var path = TestCaseGlobalVars.SampleDirectory.Combine("HeaderToolTest");
        var project = ServiceContext.Instance.Create<ICppProject>(path).Value;
        project.Parse();
        project.Setup();
        project.Build();

        // After a successful build, HeaderTool must have generated the reflection
        // glue under the module's HeaderToolGen/ tree.
        var reflectModuleDir = path.Combine("Source/ReflectModule");
        var headerToolGenExt = reflectModuleDir.Combine("HeaderToolGen/Extension");

        Assert.That(headerToolGenExt.Combine("ReflectObject.extension.h").FileExists(),
            Is.True, "HeaderTool should generate ReflectObject.extension.h");
        Assert.That(headerToolGenExt.Combine("ResetEngineExtension/ReflectObject.ext.gen.cpp").FileExists(),
            Is.True, "HeaderTool should generate ReflectObject.ext.gen.cpp (the compiled source)");
        Assert.That(headerToolGenExt.Combine("ResetEngineExtension/ReflectObject.ext.gen.h").FileExists(),
            Is.True, "HeaderTool should generate ReflectObject.ext.gen.h");

        // The remaining annotated headers exercise the wider RECLASS annotation
        // family against the parser and (for the derived class) the
        // DEFINE_DERIVED_CLASS codegen path. The plugin emits the same per-class
        // HeaderToolGen/ shape for each, so every annotated header must produce
        // its own .extension.h / .ext.gen.cpp / .ext.gen.h trio.
        var annotatedHeaders = new[]
        {
            "ReflectCharacter", // RECLASS + REFIELD + REFUNCTION
            "ReflectPlayer",    // RECLASS derived from ReflectCharacter (DEFINE_DERIVED_CLASS)
            "ReflectEnum",      // REENUM
        };
        foreach (var headerName in annotatedHeaders)
        {
            Assert.That(headerToolGenExt.Combine($"{headerName}.extension.h").FileExists(),
                Is.True, $"HeaderTool should generate {headerName}.extension.h");
            Assert.That(headerToolGenExt.Combine($"ResetEngineExtension/{headerName}.ext.gen.cpp").FileExists(),
                Is.True, $"HeaderTool should generate {headerName}.ext.gen.cpp (the compiled source)");
            Assert.That(headerToolGenExt.Combine($"ResetEngineExtension/{headerName}.ext.gen.h").FileExists(),
                Is.True, $"HeaderTool should generate {headerName}.ext.gen.h");
        }
    }
}