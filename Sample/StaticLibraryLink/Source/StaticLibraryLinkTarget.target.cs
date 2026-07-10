using ReBuildTool.ToolChain;

public class StaticLibraryLinkTarget : CppTargetRule
{
    public StaticLibraryLinkTarget()
    {
        UsedModules.Add("AppModule");
    }
}
