using ReBuildTool.Service.CompileService;
using ReBuildTool.ToolChain;

public class AppModule : CppModuleRule
{
    public override void Setup(ICppBuildContext buildContext)
    {
        TargetBuildType = BuildType.Executable;
        Dependencies.Add("GreeterModule");
    }
}
