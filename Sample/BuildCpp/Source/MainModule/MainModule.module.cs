

using ReBuildTool.Service.CompileService;
using ReBuildTool.ToolChain;

public class MainModule : CppModuleRule
{
    public override void Setup(ICppBuildContext buildContext)
    {
        TargetBuildType = BuildType.Executable;
    }
}