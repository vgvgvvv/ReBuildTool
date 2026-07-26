using ReBuildTool.Service.CompileService;
using ReBuildTool.ToolChain;

public class GreeterModule : CppModuleRule
{
    public override void Setup(ICppBuildContext buildContext)
    {
        TargetBuildType = BuildType.DynamicLibrary;
    }
}
