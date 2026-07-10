using ReBuildTool.Service.CompileService;
using ReBuildTool.ToolChain;

public class GreeterModule : CppModuleRule
{
    public GreeterModule()
    {
        TargetBuildType = BuildType.DynamicLibrary;
    }
}
