using ReBuildTool.Service.CompileService;
using ReBuildTool.ToolChain;

public class AppModule : CppModuleRule
{
    public AppModule()
    {
        TargetBuildType = BuildType.Executable;
        Dependencies.Add("MiddleModule");
        // Linking against a module only pulls in that module's own static/import library,
        // not its transitive dependencies (see CppBuilder.Process.Link.cs), so a module
        // consuming a multi-level static-lib chain must list every link-level ancestor
        // explicitly, even though include paths/defines already propagate transitively.
        Dependencies.Add("BaseModule");
    }
}
