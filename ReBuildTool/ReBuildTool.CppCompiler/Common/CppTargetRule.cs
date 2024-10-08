using ReBuildTool.Service.CompileService;

namespace ReBuildTool.ToolChain;

public abstract class CppTargetRule : ITargetInterface, IPostBuildTarget
{
    public List<string> UsedModules { get; } = new();
    
    public string TargetDirectory { get; internal set; }

    public ICppBuildContext BuildContext { get; private set; }
    
    public virtual void Setup(ICppBuildContext buildContext)
    {
        BuildContext = buildContext;
    }

    public virtual void PostBuild()
    {
    }
}