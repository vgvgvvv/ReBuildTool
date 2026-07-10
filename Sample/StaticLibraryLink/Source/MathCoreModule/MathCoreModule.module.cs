using ReBuildTool.Service.CompileService;
using ReBuildTool.ToolChain;

// Static libraries don't need dllexport/dllimport, but the module still auto-generates
// a MathCoreModule.internal.h with a MATHCOREMODULE_API macro. Declaring this define
// publicly makes that macro collapse to nothing for every consumer, matching how the
// module is actually linked (see MathCoreModule.h).
public class MathCoreModule : CppModuleRule
{
    public MathCoreModule()
    {
        TargetBuildType = BuildType.StaticLibrary;
        PublicDefines.Add("MATHCOREMODULE_BUILT_AS_STATIC");
    }
}
