using NiceIO;

namespace ReBuildTool.ToolChain.Wasm;

public partial class WasmToolchain
{

	internal override CppCompileInvocation MakeCompileInvocation(CppCompilationUnit compileUnit)
	{
		throw new NotImplementedException();
	}

	public override IEnumerable<string> CompileArgsFor(CppCompilationUnit cppCompilationInstruction)
	{
		throw new NotImplementedException();
	}
	
	public override NPath CompilerExecutableFor(NPath sourceFile)
	{
		throw new NotImplementedException();
	}
	
	public override IEnumerable<string> ToolChainDefines()
	{
		throw new NotImplementedException();
	}
	
	public override bool CanBeCompiled(NPath sourceFile)
	{
		throw new NotImplementedException();
	}
}
