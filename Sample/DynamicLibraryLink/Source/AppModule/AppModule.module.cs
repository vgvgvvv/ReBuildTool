using ReBuildTool.Service.CompileService;
using ReBuildTool.ToolChain;

public class AppModule : CppModuleRule
{
    public AppModule()
    {
        TargetBuildType = BuildType.Executable;
        Dependencies.Add("GreeterModule");
    }
}
