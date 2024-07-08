using NiceIO;

namespace ReBuildTool.ToolChain;

public abstract partial class ClangToolChain
{
	internal override CppCompileInvocation MakeCompileInvocation(CppCompilationUnit compileUnit)
	{
		
	}

	public override IEnumerable<string> CompileArgsFor(CppCompilationUnit compileUnit)
	{
		
	}

	public override IEnumerable<string> ToolChainDefines()
	{
		
	}

	public override bool CanBeCompiled(NPath sourceFile)
	{
		
	}

	public override NPath CompilerExecutableFor(NPath sourceFile)
	{
		
	}
}