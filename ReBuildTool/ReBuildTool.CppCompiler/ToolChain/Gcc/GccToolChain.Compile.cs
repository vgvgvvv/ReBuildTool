using NiceIO;

namespace ReBuildTool.ToolChain;

public partial class GccToolChain 
{
    internal override CppCompileInvocation MakeCompileInvocation(CppCompilationUnit compileUnit)
    {
        return null;
    }

    public override IEnumerable<string> CompileArgsFor(CppCompilationUnit compileUnit)
    {
        return null;
    }

    public override IEnumerable<string> ToolChainDefines()
    {
        return null;
    }

    public override bool CanBeCompiled(NPath sourceFile)
    {
        var extension = sourceFile.ExtensionWithDot;
        return extension == ".c" || 
               extension == ".cpp" || 
               extension == ".cc" || 
               extension == ".cxx";
    }

    public override NPath CompilerExecutableFor(NPath sourceFile)
    {
        return null;
    }
}