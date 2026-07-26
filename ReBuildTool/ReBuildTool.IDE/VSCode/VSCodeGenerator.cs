using System.Text.Encodings.Web;
using System.Text.Json;

using NiceIO;

using ReBuildTool.CppCompiler;
using ReBuildTool.Service.CompileService;
using ReBuildTool.Service.IDEService.VSCode;
using ReBuildTool.ToolChain;

using ResetCore.Common;

namespace ReBuildTool.IDE.VSCode;

/// <summary>
/// Emits a <c>.vscode/</c> folder (tasks.json, launch.json, c_cpp_properties.json) at the
/// project root. Every action VS Code can trigger - build, rebuild, clean, run and debug -
/// is delegated back to the very same rbt binary that generated the project, so the IDE
/// buttons drive rbt's own toolchain / incremental build instead of a separate build system.
///
/// - tasks.json: one build task per <see cref="BuildConfiguration"/> (Debug is the default
///   build, bound to Ctrl+Shift+B), a rebuild task, a clean task, plus one run task per
///   executable module.
/// - launch.json: one debug configuration per executable module (cppvsdbg on Windows,
///   cppdbg + gdb/lldb elsewhere), each with a preLaunchTask that builds the exact config.
/// - c_cpp_properties.json: points IntelliSense at the compile_commands.json that
///   <see cref="ReBuildTool.IDE.CompileCommands.CompileCommandsGenerator"/> emits.
/// </summary>
public class VSCodeGenerator : IVSCodeGenerator
{
    // The build configurations exposed in tasks.json / launch.json - mirrors rbt's
    // --BuildConfig accepted values (see CppCompilerArgs.BuildConfig / BuildConfiguration).
    private static readonly BuildConfiguration[] Configurations =
    {
        BuildConfiguration.Debug,
        BuildConfiguration.Release,
        BuildConfiguration.ReleasePlus,
        BuildConfiguration.ReleaseSize,
    };

    // Debugging targets the Debug build, so run/debug configs and their preLaunchTasks all
    // pin this config (a Release build strips the symbols the debugger needs).
    private const BuildConfiguration DebugConfiguration = BuildConfiguration.Debug;

    public VSCodeGenerator(string name, NPath outputPath)
    {
        Name = name;
        OutputPath = outputPath;
        VSCodeFolder = outputPath.Combine(".vscode");
    }

    public string Name { get; private set; }

    public string OutputPath { get; private set; }

    private NPath VSCodeFolder { get; }

    private ICppSourceProviderInterface? Source { get; set; }

    public void GenerateVSCodeProject(ICppSourceProviderInterface source, NPath output)
    {
        // Set every module up before reading any of it: a rule declares what it is
        // (TargetBuildType, and everything else) from its Setup(), so the run tasks and
        // launch configs below would otherwise see nothing but defaults.
        var setupBuilder = new CppBuilder();
        setupBuilder.SetSource(source);
        foreach (var (_, rule) in source.ModuleRules)
        {
            setupBuilder.CompleteModuleInfo(rule);
        }

        Source = source;
    }

    public bool FlushAllFiles()
    {
        if (Source == null)
        {
            throw new InvalidOperationException("GenerateVSCodeProject must be called before FlushAllFiles");
        }

        VSCodeFolder.EnsureDirectoryExists();

        WriteJson(VSCodeFolder.Combine("tasks.json"), BuildTasksJson(Source));
        WriteJson(VSCodeFolder.Combine("launch.json"), BuildLaunchJson(Source));
        WriteJson(VSCodeFolder.Combine("c_cpp_properties.json"), BuildCppPropertiesJson(Source));

        Log.Info($"Generated VS Code project at {VSCodeFolder}");
        return true;
    }

    #region tasks.json

    private object BuildTasksJson(ICppSourceProviderInterface source)
    {
        var rbtExe = RbtExecutablePath();
        var projectRoot = source.ProjectRoot.ToString().Replace("\\", "/");
        var tasks = new List<object>();

        // A build task per configuration - Debug is the default (Ctrl+Shift+B). Each launch
        // config's preLaunchTask references one of these by its exact label.
        foreach (var config in Configurations)
        {
            var isDefault = config == DebugConfiguration;
            tasks.Add(new Dictionary<string, object>
            {
                ["label"] = BuildTaskLabel(config),
                ["type"] = "process",
                ["command"] = rbtExe,
                ["args"] = RbtArgs("Build", projectRoot, config),
                ["options"] = new Dictionary<string, object> { ["cwd"] = projectRoot },
                ["group"] = new Dictionary<string, object> { ["kind"] = "build", ["isDefault"] = isDefault },
                ["problemMatcher"] = ProblemMatchers(),
            });
        }

        // Rebuild (Debug) - a full clean + build of the default config.
        tasks.Add(new Dictionary<string, object>
        {
            ["label"] = $"rbt: rebuild [{DebugConfiguration}]",
            ["type"] = "process",
            ["command"] = rbtExe,
            ["args"] = RbtArgs("ReBuild", projectRoot, DebugConfiguration),
            ["options"] = new Dictionary<string, object> { ["cwd"] = projectRoot },
            ["group"] = "build",
            ["problemMatcher"] = ProblemMatchers(),
        });

        // Clean wipes rbt's Binary/Intermedia output; it is config-agnostic (see project.Clean()).
        tasks.Add(new Dictionary<string, object>
        {
            ["label"] = "rbt: clean",
            ["type"] = "process",
            ["command"] = rbtExe,
            ["args"] = RbtArgs("Clean", projectRoot, null),
            ["options"] = new Dictionary<string, object> { ["cwd"] = projectRoot },
            ["group"] = "build",
            ["problemMatcher"] = new List<object>(),
        });

        // One "run" task per executable: build the Debug config, then launch the binary in the
        // integrated terminal (the "运行 / run" action without attaching a debugger).
        foreach (var module in ExecutableModules(source))
        {
            var programPath = ExecutableOutputPath(source, module, DebugConfiguration);
            tasks.Add(new Dictionary<string, object>
            {
                ["label"] = $"rbt: run {module.TargetName} [{DebugConfiguration}]",
                ["type"] = "process",
                ["command"] = programPath,
                ["args"] = new List<object>(),
                ["options"] = new Dictionary<string, object> { ["cwd"] = projectRoot },
                ["dependsOn"] = BuildTaskLabel(DebugConfiguration),
                ["problemMatcher"] = new List<object>(),
            });
        }

        return new Dictionary<string, object>
        {
            ["version"] = "2.0.0",
            ["tasks"] = tasks,
        };
    }

    // Flags this generator re-emits itself, so a replayed value never reaches the task twice
    // (and never contradicts what the task is meant to do): --Mode / --IDEProjectType belong to
    // the generation run, while --ProjectRoot / --TargetPlatform / --BuildConfig are written
    // explicitly below from the values the project was actually generated with.
    private static readonly HashSet<string> OverriddenArgs = new(StringComparer.OrdinalIgnoreCase)
    {
        "--Mode", "--IDEProjectType", "--ProjectRoot", "--TargetPlatform", "--BuildConfig",
    };

    // A task must build for the exact platform the project was generated for. Emitting only
    // Mode/ProjectRoot/BuildConfig made rbt fall back to the host platform, so a project
    // cross-generated with e.g. "--TargetPlatform Android" was still compiled with the host
    // (MSVC) toolchain. --TargetPlatform is therefore forwarded, and the remaining flags of
    // this rbt invocation (--TargetArch, --NDKRoot, --UseClang, ...) are replayed so the
    // cross-compile settings survive into the IDE build - same rationale as
    // CMakeGenerator.BuildRbtBuildArgs and VCProject.RbtCommand.
    // <paramref name="config"/> is null for config-agnostic modes (Clean).
    private static List<object> RbtArgs(string mode, string projectRoot, BuildConfiguration? config)
    {
        var args = new List<object>
        {
            "--Mode", mode,
            "--ProjectRoot", projectRoot,
        };

        if (config.HasValue)
        {
            args.Add("--BuildConfig");
            args.Add(config.Value.ToString());
        }

        // Only when it was explicitly set: left unset, rbt auto-detects the host platform
        // itself, and pinning it would needlessly break a .vscode/ folder shared with a
        // differently-hosted machine.
        var targetPlatform = CppCompilerArgs.Get().TargetPlatform;
        if (targetPlatform.IsSet && targetPlatform.Value != PlatformSupportType.None)
        {
            args.Add("--TargetPlatform");
            args.Add(targetPlatform.Value.ToString());
        }

        args.AddRange(PassThroughArgs());
        return args;
    }

    // The remaining command line of the rbt process generating this project, minus the flags
    // RbtArgs writes itself. Tokens are grouped the way CmdParser reads them: a "--Flag" owns
    // every following token until the next "--Flag" (list args such as --CustomDefines take
    // several values, bool switches take none), so a whole group is kept or dropped together.
    // Leading tokens that are not part of any flag are skipped - when rbt runs hosted (e.g.
    // under a test runner) the process command line starts with tokens that are not rbt args.
    private static IEnumerable<object> PassThroughArgs()
    {
        var args = Environment.GetCommandLineArgs();
        var tokens = new List<object>();
        var skipping = true;
        // args[0] is the executable path - skipped, RbtExecutablePath() is the task's command.
        for (var i = 1; i < args.Length; i++)
        {
            if (args[i].StartsWith("--", StringComparison.Ordinal))
            {
                skipping = OverriddenArgs.Contains(args[i]);
            }

            if (!skipping)
            {
                tokens.Add(args[i]);
            }
        }

        return tokens;
    }

    private static string BuildTaskLabel(BuildConfiguration config) => $"rbt: build [{config}]";

    // $gcc / $msCompile let VS Code turn compiler diagnostics from rbt's output into clickable
    // Problems entries; both are shipped with the C/C++ extension.
    private static List<object> ProblemMatchers() => new() { "$gcc", "$msCompile" };

    #endregion

    #region launch.json

    private object BuildLaunchJson(ICppSourceProviderInterface source)
    {
        var configurations = new List<object>();

        foreach (var module in ExecutableModules(source))
        {
            var programPath = ExecutableOutputPath(source, module, DebugConfiguration);
            var projectRoot = source.ProjectRoot.ToString().Replace("\\", "/");

            var launch = new Dictionary<string, object>
            {
                ["name"] = $"Debug {module.TargetName} [{DebugConfiguration}]",
                ["type"] = DebuggerType(),
                ["request"] = "launch",
                ["program"] = programPath,
                ["args"] = new List<object>(),
                ["stopAtEntry"] = false,
                ["cwd"] = projectRoot,
                ["environment"] = new List<object>(),
                ["preLaunchTask"] = BuildTaskLabel(DebugConfiguration),
            };

            // cppvsdbg (Windows) needs no MIMode; cppdbg drives gdb (Linux) or lldb (macOS).
            var miMode = DebuggerMIMode();
            if (miMode != null)
            {
                launch["MIMode"] = miMode;
                launch["externalConsole"] = false;
            }

            configurations.Add(launch);
        }

        return new Dictionary<string, object>
        {
            ["version"] = "0.2.0",
            ["configurations"] = configurations,
        };
    }

    #endregion

    #region c_cpp_properties.json

    private object BuildCppPropertiesJson(ICppSourceProviderInterface source)
    {
        // IntelliSense reads the same compile_commands.json rbt emits at the project root, so
        // include paths / defines / flags stay in exact sync with the real build.
        var configuration = new Dictionary<string, object>
        {
            ["name"] = IPlatformSupport.CurrentTargetPlatform.ToString(),
            ["compileCommands"] = "${workspaceFolder}/compile_commands.json",
            ["cStandard"] = "c17",
            ["cppStandard"] = "c++20",
            ["intelliSenseMode"] = IntelliSenseMode(),
        };

        return new Dictionary<string, object>
        {
            ["version"] = 4,
            ["configurations"] = new List<object> { configuration },
        };
    }

    #endregion

    #region helpers

    private static IEnumerable<IModuleInterface> ExecutableModules(ICppSourceProviderInterface source)
    {
        return source.ModuleRules.Values.Where(rule => rule.TargetBuildType == BuildType.Executable);
    }

    // Mirrors CppBuilder.LinkResultPath: OutputRoot/<Platform>/<Config>/<Arch>/<name><ext>.
    private static string ExecutableOutputPath(ICppSourceProviderInterface source, IModuleInterface module,
        BuildConfiguration config)
    {
        var platform = IPlatformSupport.CurrentTargetPlatform;
        var archName = BuildOptions.CreateDefault(IPlatformSupport.CurrentTargetPlatformSupport).Architecture.Name;
        var path = source.OutputRoot
            .Combine(platform.ToString())
            .Combine(config.ToString())
            .Combine(archName)
            .Combine(module.TargetName + ExecutableExtension(platform));
        return path.ToString().Replace("\\", "/");
    }

    private static string ExecutableExtension(PlatformSupportType platform)
    {
        return platform switch
        {
            PlatformSupportType.Windows => ".exe",
            PlatformSupportType.Wasm => ".html",
            _ => string.Empty,
        };
    }

    private static string DebuggerType()
    {
        return IPlatformSupport.CurrentTargetPlatform == PlatformSupportType.Windows
            ? "cppvsdbg"
            : "cppdbg";
    }

    private static string? DebuggerMIMode()
    {
        return IPlatformSupport.CurrentTargetPlatform switch
        {
            PlatformSupportType.Windows => null,
            PlatformSupportType.MacOSX => "lldb",
            PlatformSupportType.iOS => "lldb",
            _ => "gdb",
        };
    }

    private static string IntelliSenseMode()
    {
        return IPlatformSupport.CurrentTargetPlatform switch
        {
            PlatformSupportType.Windows => "windows-msvc-x64",
            PlatformSupportType.MacOSX => "macos-clang-arm64",
            PlatformSupportType.iOS => "macos-clang-arm64",
            _ => "linux-gcc-x64",
        };
    }

    // The rbt executable currently generating this project - VS Code invokes the very same
    // binary for build/run, so it stays correct regardless of where rbt lives (dev checkout
    // vs installed) without depending on $RBT_HOME / wrapper scripts. Mirrors
    // CMakeGenerator.RbtExecutablePath.
    private static string RbtExecutablePath()
    {
        var exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath))
        {
            exePath = System.Reflection.Assembly.GetEntryAssembly()?.Location;
        }
        return (exePath ?? string.Empty).Replace("\\", "/");
    }

    private static void WriteJson(NPath path, object content)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            // Paths / flags contain characters the default encoder escapes (\, +, ${...});
            // relaxed encoding keeps the files human-readable while staying valid JSON.
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };
        path.EnsureParentDirectoryExists();
        path.WriteAllText(JsonSerializer.Serialize(content, options));
    }

    #endregion
}
