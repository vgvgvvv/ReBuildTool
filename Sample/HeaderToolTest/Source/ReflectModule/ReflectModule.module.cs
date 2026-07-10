// ReflectModule — a static library whose header carries a RECLASS reflection
// annotation. HeaderTool generates reflection glue under
// ReflectModule/HeaderToolGen/Extension/, which the framework auto-injects
// (SourceDirectories + PublicIncludePaths) after the tool runs so the generated
// .ext.gen.cpp is compiled and .extension.h is #include-able.
using ReBuildTool.Service.CompileService;
using ReBuildTool.ToolChain;

public class ReflectModule : CppModuleRule
{
    public ReflectModule()
    {
        TargetBuildType = BuildType.StaticLibrary;
    }
}
