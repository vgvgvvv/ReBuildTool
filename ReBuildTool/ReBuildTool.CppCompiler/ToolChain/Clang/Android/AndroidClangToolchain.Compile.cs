namespace ReBuildTool.ToolChain.Android;

public partial class AndroidClangToolchain
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