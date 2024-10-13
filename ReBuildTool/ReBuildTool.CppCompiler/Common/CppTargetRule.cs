using ReBuildTool.Service.CompileService;

namespace ReBuildTool.ToolChain;

public abstract class CppTargetRule : ITargetInterface, IPostBuildTarget
{
    public virtual List<string> UsedModules { get; } = new();
    
    public string TargetDirectory { get; internal set; }

    public virtual List<ITargetCompilePlugin> Plugins { get; } = new();

    public virtual Dictionary<string, string> CustomInfo { get; } = new();

    public ICppBuildContext BuildContext { get; private set; }
    
    public virtual void Setup(ICppBuildContext buildContext)
    {
        BuildContext = buildContext;
    }

    public virtual void PostBuild()
    {
    }
}