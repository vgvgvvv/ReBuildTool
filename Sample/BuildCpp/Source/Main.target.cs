

using ReBuildTool.ToolChain;

public class MainTarget : TargetRule
{
    public MainTarget()
    {
        UsedModules.Add("MainModule");
    }
} 