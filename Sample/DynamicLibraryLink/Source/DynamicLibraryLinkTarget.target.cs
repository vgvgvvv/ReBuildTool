using ReBuildTool.ToolChain;

public class DynamicLibraryLinkTarget : CppTargetRule
{
    public DynamicLibraryLinkTarget()
    {
        UsedModules.Add("AppModule");
    }
}
