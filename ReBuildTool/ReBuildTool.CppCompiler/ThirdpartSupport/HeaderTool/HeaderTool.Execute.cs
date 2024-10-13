using ReBuildTool.CppCompiler;
using ReBuildTool.Service.Global;
using ReBuildTool.ToolChain;

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
    
    public IEnumerable<string> GetCmdArgs(CppTargetRule targetRule, CppBuilder builder)
    {
        var projectArgs = CppCompilerArgs.Get();

        yield return $"projectPath={projectArgs.ProjectRoot}";
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

        yield return $"pluginDlls={string.Join('|', headerToolTarget.PluginDlls)}";
        yield return $"plugins={string.Join('|', headerToolTarget.PluginNames)}";
        yield return "projectType=Custom";
        
        foreach (var extraArg in headerToolTarget.ExtraArgs)
        {
            yield return extraArg;
        }
        
        // customProject
    }

}