using NiceIO;
using ReBuildTool.ToolChain.SDK;
using ResetCore.Common;

namespace ReBuildTool.ToolChain;

public abstract partial class ClangToolChain
{
	internal override CppCompileInvocation MakeCompileInvocation(CppCompilationUnit compileUnit)
	{
		var invocation = new CppCompileInvocation(compileUnit);
		invocation.ProgramName = CompilerExecutableFor(compileUnit.SourceFile);
		invocation.EnvVars.AddRange(EnvVars());
		invocation.Arguments.AddRange(CompileArgsFor(compileUnit));
		return invocation;
	}

	// public abstract IEnumerable<string> CompileArgsFor(CppCompilationUnit compileUnit);

	//public abstract IEnumerable<string> ToolChainDefines();

	public override bool CanBeCompiled(NPath sourceFile)
	{
		var ex = sourceFile.ExtensionWithDot;
		return ex == ".cpp" || ex == ".c" || ex == ".cc" || ex == ".cxx" || ex == ".asm";
	}
	
	public override NPath CompilerExecutableFor(NPath sourceFile)
	{
		return ClangSdk.GetCompiler();
	}


}