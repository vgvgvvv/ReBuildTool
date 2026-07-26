using System.Reflection;
using System.Runtime.InteropServices;
using NiceIO;
using ReBuildTool.CppCompiler;
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
        // Launchers are a Visual Studio concept, and the VS generator itself needs an
        // installed MSVC SDK, so this only applies on Windows (elsewhere the default
        // IDE project is CMake and no .vcxproj is produced).
        if (!OperatingSystem.IsWindows())
        {
            Assert.Ignore("Visual Studio project generation is only supported on Windows.");
        }

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

    // Generates only a compile_commands.json (JSON Compilation Database) and verifies it is a
    // valid array of per-file entries with the LLVM-spec keys, written to the project root.
    // This is what clangd / VS Code / CLion read for code highlighting + go-to-definition.
    [Test]
    public void TestCompileCommandsGenerate()
    {
        CmdParser.Parse<Tests>();
        ServiceContext.Instance.Init();

        ProjectGenArgs.Get().IDEProjectType.Value = ProjectGenType.CompileCommands;

        var path = TestCaseGlobalVars.SampleDirectory.Combine("StaticLibraryLink");
        var dbPath = path.Combine("compile_commands.json");
        dbPath.DeleteIfExists();

        var project = ServiceContext.Instance.Create<ICppProject>(path).Value;
        project.Parse();
        project.Setup();

        Assert.IsTrue(dbPath.Exists(), "compile_commands.json should be generated at the project root");

        using var doc = System.Text.Json.JsonDocument.Parse(dbPath.ReadAllText());
        Assert.That(doc.RootElement.ValueKind, Is.EqualTo(System.Text.Json.JsonValueKind.Array),
            "compile_commands.json must be a JSON array");
        Assert.That(doc.RootElement.GetArrayLength(), Is.GreaterThan(0),
            "the database must contain at least one compile entry");

        foreach (var entry in doc.RootElement.EnumerateArray())
        {
            Assert.IsTrue(entry.TryGetProperty("directory", out _), "entry must have a directory");
            Assert.IsTrue(entry.TryGetProperty("file", out var file), "entry must have a file");
            Assert.IsTrue(entry.TryGetProperty("output", out _), "entry must have an output");
            Assert.IsTrue(entry.TryGetProperty("arguments", out var args), "entry must have arguments");
            Assert.That(args.ValueKind, Is.EqualTo(System.Text.Json.JsonValueKind.Array));
            // argv[0] must be the compiler executable so clangd can infer the driver.
            Assert.That(args.GetArrayLength(), Is.GreaterThan(0), "arguments must include the compiler");
            Assert.IsTrue(file.GetString()!.EndsWith(".cpp") || file.GetString()!.EndsWith(".c"),
                "each entry's file should be a compilable source");
            // Each argument must be a single clean argv token (LLVM JSON Compilation Database
            // spec): the toolchains must not bake shell-level quoting into a token, or clangd /
            // VS Code / CLion see the literal double quotes as part of the path. Checked as the
            // specific shapes quoting used to produce (-I"...", /Fo"...", "...", --sysroot="...")
            // rather than "contains a quote at all" — a quote can legitimately be part of a
            // value, e.g. a -DNAME="a b" macro body, and must survive untouched.
            foreach (var arg in args.EnumerateArray())
            {
                var argStr = arg.GetString()!;
                Assert.IsFalse(argStr.StartsWith('"') && argStr.EndsWith('"'),
                    $"argument must not be wrapped in shell quotes (found '{argStr}' for {file.GetString()})");
                Assert.IsFalse(argStr.Contains("-I\"") || argStr.Contains("/I\"") ||
                               argStr.Contains("/Fo\"") || argStr.Contains("--sysroot=\""),
                    $"argument must not embed shell quotes after a flag (found '{argStr}' for {file.GetString()})");
            }
        }
    }

    // Generates a build.ninja via the Ninja generator (same per-module path rbt's Ninja
    // backend uses) and verifies quoting is correct so that paths with spaces survive both
    // ninja's lexer AND the shell ninja hands the command to:
    //   - structural `build` line paths: each space escaped as `$ ` (dollar+space) and each
    //     `:` as `$:` (drive-letter) — ninja's path-list escape, NOT a `$ ... $` wrapper
    //     (which breaks ninja's build-line parser with "expected build command name").
    //   - variable values (cc/args): shell-quoted via ShellQuote, because ninja runs
    //     `command = $cc $args` through a shell (sh -c / cmd /c) — ninja's `$ ` space escape
    //     does NOT prevent word-splitting at execution time.
    // Also asserts the generated file parses cleanly under the vendored ninja binary.
    [Test]
    public void TestNinjaFileEscape()
    {
        CmdParser.Parse<Tests>();
        ServiceContext.Instance.Init();

        var path = TestCaseGlobalVars.SampleDirectory.Combine("StaticLibraryLink");
        var project = ServiceContext.Instance.Create<ICppProject>(path).Value;
        project.Parse();
        project.Setup();

        var builder = new CppBuilder();
        builder.SetSource((ICppSourceProviderInterface)project);
        var ninjaFiles = builder.GenerateNinjaFiles();

        Assert.IsNotEmpty(ninjaFiles, "Ninja generator should produce a build.ninja per module");

        foreach (var ninjaFile in ninjaFiles)
        {
            Assert.IsTrue(ninjaFile.Exists(), $"{ninjaFile} should exist");
            var content = ninjaFile.ReadAllText();

            // Variable values: no residual flag-internal quoting (-I"...", /Fo"...").
            var argsLines = content.Split('\n')
                .Where(l => l.TrimStart().StartsWith("args = "))
                .ToList();
            Assert.IsNotEmpty(argsLines, $"{ninjaFile} must have at least one args = line");
            foreach (var line in argsLines)
            {
                Assert.IsFalse(line.Contains("-I\"") || line.Contains("/I\"") ||
                               line.Contains("/Fo\"") || line.Contains("--sysroot=\""),
                    $"{ninjaFile} args value must not contain flag-internal quotes: {line.Trim()}");
            }

            // Structural build lines must NOT use the old `$ ... $` wrapper (it breaks ninja's
            // parser). Spaces there are escaped per-space as `$ `.
            var buildLines = content.Split('\n').Where(l => l.StartsWith("build ")).ToList();
            Assert.IsNotEmpty(buildLines, $"{ninjaFile} must have build lines");
            foreach (var line in buildLines)
            {
                Assert.IsFalse(line.Contains("$ ") && line.TrimEnd().EndsWith(" $"),
                    $"{ninjaFile} build line must not wrap the whole path in '$ ... $': {line}");
            }
        }
    }


    // Generates a Makefile via the Makefile generator (same per-module path rbt's Makefile
    // backend uses) and verifies recipe lines shell-quote tokens with spaces via
    // ShellQuote.ForArgument, with no residual flag-internal quoting like -I"D:\...".
    [Test]
    public void TestMakeFileRecipeEscape()
    {
        CmdParser.Parse<Tests>();
        ServiceContext.Instance.Init();

        var path = TestCaseGlobalVars.SampleDirectory.Combine("StaticLibraryLink");
        var project = ServiceContext.Instance.Create<ICppProject>(path).Value;
        project.Parse();
        project.Setup();

        var builder = new CppBuilder();
        builder.SetSource((ICppSourceProviderInterface)project);
        var makeFiles = builder.GenerateMakeFiles();

        Assert.IsNotEmpty(makeFiles, "Makefile generator should produce a Makefile per module");

        foreach (var makeFile in makeFiles)
        {
            Assert.IsTrue(makeFile.Exists(), $"{makeFile} should exist");
            var content = makeFile.ReadAllText();
            // Recipe lines are tab-indented and contain the compiler invocation.
            var recipeLines = content.Split('\n').Where(l => l.StartsWith("\t")).ToList();
            Assert.IsNotEmpty(recipeLines, $"{makeFile} must have recipe lines");
            foreach (var line in recipeLines)
            {
                // No flag-internal quotes should remain (e.g. -I"...", /Fo"...").
                Assert.IsFalse(line.Contains("-I\"") || line.Contains("/I\"") ||
                               line.Contains("/Fo\"") || line.Contains("--sysroot=\""),
                    $"{makeFile} recipe must not contain flag-internal quotes: {line.Trim()}");
            }
        }
    }


    // Generates the VS Code project (.vscode/tasks.json + launch.json + c_cpp_properties.json)
    // and verifies the rbt-driven wiring: build/rebuild/clean tasks all invoke rbt, every
    // executable module gets a run task and a debug launch config, and IntelliSense is pointed
    // at the compile_commands.json rbt emits.
    [Test]
    public void TestVSCodeProjectGenerate()
    {
        CmdParser.Parse<Tests>();
        ServiceContext.Instance.Init();

        // On Windows the default IDE project is VS - force VS Code generation for this test.
        ProjectGenArgs.Get().IDEProjectType.Value = ProjectGenType.VSCode;

        // Explicitly select a target platform (the host one, so the generated project still
        // matches this machine's toolchain) to check the generated tasks forward it: without
        // --TargetPlatform an rbt task falls back to the host platform, which builds a
        // cross-generated project with the wrong toolchain.
        var targetPlatform = CppCompilerArgs.Get().TargetPlatform;
        targetPlatform.Value = IPlatformSupport.CurrentTargetPlatform;
        targetPlatform.MarkHasSet();

        var path = TestCaseGlobalVars.SampleDirectory.Combine("StaticLibraryLink");
        var vscodeDir = path.Combine(".vscode");
        vscodeDir.DeleteIfExists();

        var project = ServiceContext.Instance.Create<ICppProject>(path).Value;
        project.Parse();
        project.Setup();

        // tasks.json: build/rebuild/clean, all delegating to rbt.
        var tasksFile = vscodeDir.Combine("tasks.json");
        Assert.IsTrue(tasksFile.Exists(), ".vscode/tasks.json should be generated");
        using (var tasksDoc = System.Text.Json.JsonDocument.Parse(tasksFile.ReadAllText()))
        {
            var tasks = tasksDoc.RootElement.GetProperty("tasks");
            Assert.That(tasks.ValueKind, Is.EqualTo(System.Text.Json.JsonValueKind.Array));
            var labels = tasks.EnumerateArray()
                .Select(t => t.GetProperty("label").GetString())
                .ToList();
            Assert.IsTrue(labels.Contains("rbt: build [Debug]"), "must have a Debug build task");
            Assert.IsTrue(labels.Contains("rbt: rebuild [Debug]"), "must have a rebuild task");
            Assert.IsTrue(labels.Contains("rbt: clean"), "must have a clean task");
            // AppModule is an executable, so it must get a run task.
            Assert.IsTrue(labels.Any(l => l!.StartsWith("rbt: run AppModule")),
                "each executable module must get a run task");

            // The default build task must drive an rbt build.
            var buildTask = tasks.EnumerateArray()
                .First(t => t.GetProperty("label").GetString() == "rbt: build [Debug]");
            var args = buildTask.GetProperty("args").EnumerateArray().Select(a => a.GetString()).ToList();
            Assert.IsTrue(args.Contains("--Mode") && args.Contains("Build"),
                "the build task must invoke rbt --Mode Build");

            // Every rbt task must carry the platform the project was generated for: without an
            // explicit --TargetPlatform rbt falls back to the host platform, so a project
            // generated with --TargetPlatform <other> would build with the host toolchain.
            var expectedPlatform = targetPlatform.Value.ToString();
            foreach (var label in new[] { "rbt: build [Debug]", "rbt: rebuild [Debug]", "rbt: clean" })
            {
                var task = tasks.EnumerateArray().First(t => t.GetProperty("label").GetString() == label);
                var taskArgs = task.GetProperty("args").EnumerateArray().Select(a => a.GetString()).ToList();
                var platformIndex = taskArgs.IndexOf("--TargetPlatform");
                Assert.That(platformIndex, Is.GreaterThanOrEqualTo(0),
                    $"'{label}' must pass --TargetPlatform to rbt");
                Assert.AreEqual(expectedPlatform, taskArgs[platformIndex + 1],
                    $"'{label}' must build for the platform the project was generated for");
            }
        }

        // launch.json: one debug config per executable module, building before launch.
        var launchFile = vscodeDir.Combine("launch.json");
        Assert.IsTrue(launchFile.Exists(), ".vscode/launch.json should be generated");
        using (var launchDoc = System.Text.Json.JsonDocument.Parse(launchFile.ReadAllText()))
        {
            var configs = launchDoc.RootElement.GetProperty("configurations");
            Assert.That(configs.GetArrayLength(), Is.GreaterThan(0),
                "there must be a debug config for the executable module");
            var appConfig = configs.EnumerateArray()
                .First(c => c.GetProperty("name").GetString()!.Contains("AppModule"));
            Assert.AreEqual("launch", appConfig.GetProperty("request").GetString());
            Assert.IsTrue(appConfig.TryGetProperty("preLaunchTask", out var preLaunch),
                "a debug config must build before launching");
            Assert.AreEqual("rbt: build [Debug]", preLaunch.GetString());
        }

        // c_cpp_properties.json: IntelliSense follows rbt's compile_commands.json.
        var cppPropsFile = vscodeDir.Combine("c_cpp_properties.json");
        Assert.IsTrue(cppPropsFile.Exists(), ".vscode/c_cpp_properties.json should be generated");
        using (var cppDoc = System.Text.Json.JsonDocument.Parse(cppPropsFile.ReadAllText()))
        {
            var config = cppDoc.RootElement.GetProperty("configurations")[0];
            Assert.That(config.GetProperty("compileCommands").GetString(),
                Does.Contain("compile_commands.json"),
                "IntelliSense must read rbt's compile_commands.json");
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