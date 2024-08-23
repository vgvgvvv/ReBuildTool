using NiceIO;

namespace ReBuildTool.ToolChain;

public abstract partial class ClangToolChain
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
		return false;
	}

	public override NPath CompilerExecutableFor(NPath sourceFile)
	{
		return null;
	}
}