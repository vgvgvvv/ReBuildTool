namespace ReBuildTool.ToolChain;

public abstract class TargetRule
{
    public List<string> UsedModules { get; } = new();
    
    public string TargetDirectory { get; internal set; }
}