namespace ReBuildTool.ToolChain;

public partial class LinuxClangToolchain 
{
    public override IEnumerable<string> CompileArgsFor(CppCompilationUnit compileUnit)
    {
        throw new NotImplementedException();
    }

    public override IEnumerable<string> ToolChainDefines()
    {
        throw new NotImplementedException();
    }
}