using ReBuildTool.Service.CompileService;

namespace ReBuildTool.ToolChain;

public abstract class TargetRule : ITargetInterface
{
    public List<string> UsedModules { get; } = new();
    
    public string TargetDirectory { get; internal set; }
}