using ReBuildTool.ToolChain;

public class MultiModuleChainTarget : CppTargetRule
{
    public MultiModuleChainTarget()
    {
        UsedModules.Add("AppModule");
    }
}
