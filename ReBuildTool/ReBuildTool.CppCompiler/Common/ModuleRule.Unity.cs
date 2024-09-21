using ReBuildTool.Service.CompileService;

namespace ReBuildTool.ToolChain;

public abstract class UnityModuleRule : ModuleRule
{
    public override BuildType TargetBuildType
    {
        get
        {
            if (IPlatformSupport.CurrentTargetPlatformSupport is iOSPlatformSupport)
            {
                return BuildType.StaticLibrary;
            }
            else
            {
                return BuildType.DynamicLibrary;
            }
        }
    }
}