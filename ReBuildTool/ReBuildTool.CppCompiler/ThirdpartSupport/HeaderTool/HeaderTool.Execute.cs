using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ReBuildTool.CppCompiler;
using ReBuildTool.Service.Global;
using ReBuildTool.ToolChain;
using ResetCore.Common;

namespace ReBuildTool.Service.CompileService.HeaderTool;

public partial class HeaderToolPluginSupport
{

    private void RunHeaderTool(CppTargetRule targetRule, CppBuilder builder)
    {
        // Run ResetHeaderTool with its working directory set to the project root.
        // RHT resolves plugin DLLs by searching Environment.CurrentDirectory first
        // (then the dllSearchPath entries), and its Directory.GetFiles throws on a
        // non-existent search dir. Setting the workspace to the project root makes
        // project-relative PluginDlls (e.g. "Plugins/Foo.dll") resolve from cwd and
        // avoids the crash when the caller's own cwd lacks those dirs.
        var projectRoot = builder.CurrentSource.ProjectRoot;
        var shell = Shell.Create()
            .WithProgram(HeaderToolExePath)
            .WithWorkspace(projectRoot)
            .WithArguments(GetCmdArgs(targetRule, builder))
            .Execute()
            .WaitForEnd();

        if (shell.Process.ExitCode != 0)
        {
            throw new Exception("run header tool failed");
        }
    }
    
    private IEnumerable<string> GetCmdArgs(CppTargetRule targetRule, CppBuilder builder)
    {
        // Prefer the source provider's ProjectRoot (the actual project directory,
        // e.g. the sample root) over the global --projectroot CLI arg (which
        // defaults to Environment.CurrentDirectory and is wrong under the test
        // harness, where cwd is the test bin output dir). ResetHeaderTool uses
        // projectPath both to locate sources and as its own working context.
        var projectRoot = builder.CurrentSource.ProjectRoot.ToString();
        yield return $"projectPath={projectRoot}";
        yield return "projectType=ReBuildTool";
        if (builder.CurrentBuildOption.Configuration == BuildConfiguration.Debug)
        {
            yield return "debug=true";
        }

        if (builder.CurrentPlatformSupport is WindowsPlatformSupport)
        {
            yield return "targetplatform=Win64";
        }
        else if(builder.CurrentPlatformSupport is MacOSXPlatformSupport)
        {
            yield return "targetplatform=Mac";
        }
        else if(builder.CurrentPlatformSupport is LinuxPlatformSupport)
        {
            yield return "targetplatform=Linux";
        }
        else if(builder.CurrentPlatformSupport is iOSPlatformSupport)
        {
            yield return "targetplatform=IOS";
        }
        else if (builder.CurrentPlatformSupport is AndroidPlatformSupport)
        {
            yield return "targetplatform=Android";
        }
        else
        {
            throw new Exception("not support platform");
        }

        var headerToolTarget = targetRule as IHeaderToolTarget;

        // ResetHeaderTool resolves each plugin DLL (from pluginDlls=...) by
        // recursively searching every dllSearchPath entry (plus its own cwd).
        // Include both the HeaderToolRoot (where the tool + built-in plugins live)
        // and the project root, so PluginDlls given as project-relative paths
        // (e.g. "Plugins/MyExtension.dll") resolve regardless of the tool's cwd.
        var searchRoots = new List<string>();
        var headerToolRoot = HeaderToolRoot.ToString();
        if (!string.IsNullOrEmpty(headerToolRoot))
        {
            searchRoots.Add(headerToolRoot);
        }
        if (!string.IsNullOrEmpty(projectRoot)
            && !searchRoots.Contains(projectRoot))
        {
            searchRoots.Add(projectRoot);
        }
        if (searchRoots.Count > 0)
        {
            yield return $"dllSearchPath={string.Join('|', searchRoots)}";
        }
        if (headerToolTarget.PluginDlls != null && headerToolTarget.PluginDlls.Count > 0)
        {
            yield return $"pluginDlls={string.Join('|', headerToolTarget.PluginDlls)}";
            if (headerToolTarget.PluginNames != null && headerToolTarget.PluginNames.Count > 0)
            {
                yield return $"plugins={string.Join('|', headerToolTarget.PluginNames)}";

            }
        }

        foreach (var extraArg in headerToolTarget.ExtraArgs)
        {
            yield return extraArg;
        }
        
        // customProject
    }

  
    private void GenerateProjectInfoForHeaderTool(CppTargetRule targetRule, CppBuilder builder)
    {
        var projectRoot = HeaderToolRoot.Combine("ProjectInfo").EnsureDirectoryExists();
        // Serialize only the fields HeaderTool actually reads (Public/Private
        // IncludePaths, SourceDirectories, ModuleDirectory, Dependencies,
        // TargetBuildType) — see ResetHeaderTool ReBuildToolModule.ParseModule.
        // Full-object serialization trips on NPath properties (e.g. Parent on an
        // empty path) and on framework-only fields like BuildContext.
        var jsonSettings = new JsonSerializerSettings
        {
            Error = (_, args) =>
            {
                // The member that failed (e.g. NPath.Parent on an empty path,
                // or a framework-only field) is not one HeaderTool reads, so we
                // skip it rather than abort. Log so the skip is diagnosable.
                Log.Warning($"HeaderTool module-info serialization: skipping member " +
                            $"\"{args.ErrorContext.Member}\" of module \"{args.CurrentObject}\": {args.ErrorContext.Error.Message}");
                args.ErrorContext.Handled = true;
            }
        };
        foreach (var (key, module) in CodeSource.ModuleRules)
        {
            var moduleFolder = projectRoot.Combine(module.TargetName).EnsureDirectoryExists();
            var moduleInfo = moduleFolder.Combine("ModuleInfo.json");

            // Serialize only the fields ResetHeaderTool reads (ParseModule),
            // with non-existent paths filtered out. ResetHeaderTool does
            // Directory.GetFiles on every IncludePath/SourceDirectory — a
            // non-existent path (e.g. HeaderToolGen/Extension which is generated
            // later by HeaderTool itself) crashes it with DirectoryNotFoundException.
            // This projection avoids that without modifying the rule's own lists.
            var rule = module as CppModuleRule;
            var dump = new
            {
                TargetBuildType = module.TargetBuildType,
                TargetName = module.TargetName,
                PublicIncludePaths = module.PublicIncludePaths.Where(p => System.IO.Directory.Exists(p)).ToList(),
                PrivateIncludePaths = module.PrivateIncludePaths.Where(p => System.IO.Directory.Exists(p)).ToList(),
                PublicDefines = module.PublicDefines,
                PrivateDefines = module.PrivateDefines,
                PublicCompileFlags = module.PublicCompileFlags,
                PrivateCompileFlags = module.PrivateCompileFlags,
                PublicLinkFlags = module.PublicLinkFlags,
                PrivateLinkFlags = module.PrivateLinkFlags,
                PublicStaticLibraries = module.PublicStaticLibraries,
                PrivateStaticLibraries = module.PrivateStaticLibraries,
                PublicDynamicLibraries = module.PublicDynamicLibraries,
                PrivateDynamicLibraries = module.PrivateDynamicLibraries,
                PublicLibraryDirectories = module.PublicLibraryDirectories,
                PrivateLibraryDirectories = module.PrivateLibraryDirectories,
                SourceDirectories = module.SourceDirectories.Where(p => System.IO.Directory.Exists(p)).ToList(),
                SourceFiles = rule?.SourceFiles.Where(p => System.IO.File.Exists(p)).ToList() ?? new List<string>(),
                ExcludeDirectories = rule?.ExcludeDirectories ?? new List<string>(),
                ExcludeFiles = rule?.ExcludeFiles ?? new List<string>(),
                Dependencies = module.Dependencies,
                ModuleDirectory = module.ModuleDirectory,
                IsSupport = module.IsSupport,
            };
            moduleInfo.WriteAllText(JsonConvert.SerializeObject(dump, Formatting.Indented, jsonSettings));
        }

    }

}