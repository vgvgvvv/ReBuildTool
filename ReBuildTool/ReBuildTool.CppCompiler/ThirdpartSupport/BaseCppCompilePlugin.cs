using ReBuildTool.ToolChain;

namespace ReBuildTool.Service.CompileService;

public abstract class BaseCppCompilePlugin
{
    public virtual void PreCompile(CppBuilder builder)
    {
    }

    public virtual void PostCompile(CppBuilder builder)
    {
    }
}

public abstract class BaseCppTargetCompilePlugin : ITargetCompilePlugin
{

    public virtual void Setup()
    {
    }
    
    public virtual void PreCompile(CppTargetRule targetRule, CppBuilder builder)
    {
    }

    public virtual void PostCompile(CppTargetRule targetRule,CppBuilder builder)
    {
    }
}