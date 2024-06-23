namespace ReBuildTool.ToolChain;

public class ModuleRule
{
    public string TargetName => GetType().Name;
    
    public List<string> PublicIncludePaths { get; } = new();

    public List<string> PrivateIncludePaths { get; } = new();

    public List<string> SourceDirectory { get; } = new();

    public List<string> Dependency { get; } = new();

}