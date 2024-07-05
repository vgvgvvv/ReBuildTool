using System.Runtime.InteropServices;
using NiceIO;
using ReBuildTool.Service.Global;
using ReBuildTool.Service.CommandGroup;
using ReCSharpCommon.Platform;

namespace ReBuildTool.Actions;

public class ResetHeaderTool
{
    
    public static NPath RHTDir => GlobalCmd.CommonCommand
        .GetIntermediaPath()
        .Combine("ResetHeaderTool")
        .EnsureDirectoryExists();
    public static NPath RHTBinaryDir => RHTDir.Combine("Binary").EnsureDirectoryExists();
    
    
    [ActionDefine("ResetHeaderTool.Init")]
    public static void InitHeaderTool()
    {
        var csprojLocation = RHTDir.Combine("ResetHeaderTool/ResetHeaderTool/ResetHeaderTool.csproj");
        var rhtName = "ResetHeaderTool";
        Git.GetFromGit("git@github.com:vgvgvvv/ResetHeaderTool.git", rhtName, RHTDir);

        void BuildForPlatformAndArch(PlatformType platform, Architecture arch)
        {
            var publicArgs = new List<string>()
            {
                "publish",
                csprojLocation,
                "-r", PlatformUtils.GetDotnetRuntime(platform, arch),
                "-o", GetHeaderToolFolder(platform, arch)
            };
            
            SimpleExec.Command.Run("dotnet", publicArgs, RHTDir);

        }
        
        BuildForPlatformAndArch(PlatformType.Windows, Architecture.X64);
        BuildForPlatformAndArch(PlatformType.Linux, Architecture.X64);
        BuildForPlatformAndArch(PlatformType.MacOS, Architecture.X64);
        BuildForPlatformAndArch(PlatformType.MacOS, Architecture.Arm64);
    }
    
    [ActionDefine("ResetHeaderTool.GetPlugin")]
    public static void GetHeaderToolPlugin(string targetUrl, string name, string csLoc)
    {
        var csprojLocation = RHTDir.Combine(csLoc);
        Git.GetFromGit(targetUrl, name, RHTDir);
        
        var publicArgs = new List<string>()
        {
            "publish",
            csprojLocation,
            "-o", GetHeaderToolPluginFolder(name)
        };
            
        SimpleExec.Command.Run("dotnet", publicArgs, RHTDir);
    }

    [ActionDefine("ResetHeaderTool.Run")]
    public static void RunHeaderTool(string projectRoot, string projectType, string plugins, string args)
    {
        var executeArgs = new List<string>()
        {
            $"projectPath={projectRoot}",
            $"projectType={projectType}",
            $"pluginDlls={string.Join(",", plugins.Split(",").Select(n=>GetHeaderToolPluginDll(n.Trim())))}",
            $"plugins={plugins}",
        };
        executeArgs.AddRange(args.Split(" "));
        
        SimpleExec.Command.Run(GetCurrentHeaderToolExe(), executeArgs, projectRoot);
    }


    private static NPath GetHeaderToolFolder(PlatformType platform, Architecture arch)
    {
        return RHTBinaryDir.Combine(
            PlatformUtils.GetPlatformFolder(platform, arch), "ResetHeaderTool");
    }
    
    private static NPath GetCurrentHeaderToolFolder()
    {
        return GetHeaderToolFolder(PlatformUtils.GetPlatform(), PlatformUtils.GetArch());
    }

    private static NPath GetCurrentHeaderToolExe()
    {
        return GetHeaderToolFolder(PlatformUtils.GetPlatform(), PlatformUtils.GetArch())
            .Combine($"ResetHeaderTool{PlatformUtils.GetPlatformExecutableEx(PlatformUtils.GetPlatform())}");
    }

    private static NPath GetHeaderToolPluginFolder(string pluginName)
    {
        return RHTBinaryDir.Combine("Common", pluginName);
    }
    private static NPath GetHeaderToolPluginDll(string pluginName)
    {
        return GetHeaderToolPluginFolder(pluginName).Combine($"{pluginName}.dll");
    }
    
    
    
}