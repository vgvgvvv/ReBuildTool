

using ReBuildTool.ToolChain;

public class MainTarget : CppTargetRule
{
    public MainTarget()
    {
        UsedModules.Add("MainModule");
    }
} 