using ReBuildTool.ToolChain;

public class PackageConsumerTarget : CppTargetRule
{
    public PackageConsumerTarget()
    {
        UsedModules.Add("AppModule");
    }
}
