using ReBuildTool.Service.CompileService;
using ReBuildTool.ToolChain;

public class MiddleModule : CppModuleRule
{
    public override void Setup(ICppBuildContext buildContext)
    {
        TargetBuildType = BuildType.StaticLibrary;
        PublicDefines.Add("MIDDLEMODULE_BUILT_AS_STATIC");
        Dependencies.Add("BaseModule");
    }
}
