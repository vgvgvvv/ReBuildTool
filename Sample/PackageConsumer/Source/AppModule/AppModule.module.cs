using ReBuildTool.Service.CompileService;
using ReBuildTool.ToolChain;

public class AppModule : CppModuleRule
{
    public override void Setup(ICppBuildContext buildContext)
    {
        TargetBuildType = BuildType.Executable;
        // GeometryModule is not in this project's Source/ - it comes from the GeometryPackage
        // package declared in RBTPackage.json. A package's module is depended on by name, exactly
        // like a local one.
        Dependencies.Add("GeometryModule");
    }
}
