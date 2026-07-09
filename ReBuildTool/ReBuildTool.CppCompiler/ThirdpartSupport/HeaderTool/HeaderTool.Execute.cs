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
        var shell = Shell.Create()
            .WithProgram(HeaderToolExePath)
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
        var projectArgs = CppCompilerArgs.Get();

        yield return $"projectPath={projectArgs.ProjectRoot}";
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

        // Use the resolved HeaderToolRoot property (which falls back to
        // IntermediaFolder/ResetHeaderTool) rather than the raw CLI arg, so the
        // search path is never empty when --headertoolroot is not passed.
        var searchRoot = HeaderToolRoot.ToString();
        if (!string.IsNullOrEmpty(searchRoot))
        {
            yield return $"dllSearchPath={searchRoot}";
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

            moduleInfo.WriteAllText(JsonConvert.SerializeObject(module, Formatting.Indented, jsonSettings));
        }

    }

}