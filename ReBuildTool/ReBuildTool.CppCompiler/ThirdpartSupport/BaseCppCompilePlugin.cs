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