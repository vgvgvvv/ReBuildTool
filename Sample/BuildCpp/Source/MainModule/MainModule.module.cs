

using ReBuildTool.Service.CompileService;
using ReBuildTool.ToolChain;

public class MainModule : CppModuleRule
{
    public MainModule()
    {
        TargetBuildType = BuildType.Executable;
    }
}