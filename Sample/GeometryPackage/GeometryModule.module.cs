using ReBuildTool.Service.CompileService;
using ReBuildTool.ToolChain;

// A package ships ordinary module rules. Nothing here says "package": once restore has put the
// directory on disk, rbt globs this file into the very same CompileRules.dll as the consuming
// project's own rules, so the module behaves exactly like a local one.
//
// Note the module is named GeometryModule while the package is named GeometryPackage - a package
// is a unit of distribution and may contain any number of modules under any names.
public class GeometryModule : CppModuleRule
{
    public override void Setup(ICppBuildContext buildContext)
    {
        TargetBuildType = BuildType.StaticLibrary;
        // Collapses the auto-generated GEOMETRYMODULE_API macro to nothing, matching how a static
        // library is actually linked - same reasoning as Sample/StaticLibraryLink.
        PublicDefines.Add("GEOMETRYMODULE_BUILT_AS_STATIC");
    }
}
