using ReBuildTool.Service.CompileService;
using ReBuildTool.ToolChain;

public class BaseModule : CppModuleRule
{
    public BaseModule()
    {
        TargetBuildType = BuildType.StaticLibrary;
        PublicDefines.Add("BASEMODULE_BUILT_AS_STATIC");
        // Public defines propagate to every module that (transitively) depends on this one.
        PublicDefines.Add("HAS_BASE_MODULE");
    }
}
