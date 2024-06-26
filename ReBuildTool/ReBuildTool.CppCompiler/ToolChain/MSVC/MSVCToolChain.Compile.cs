using NiceIO;
using ResetCore.Common;

namespace ReBuildTool.ToolChain;

public partial class MSVCToolChain 
{
	internal override CppCompileInvocation MakeCompileInvocation(CppCompilationUnit compileUnit)
	{
		var invocation = new CppCompileInvocation();
		invocation.ProgramName = CompilerExecutableFor(compileUnit.SourceFile);
		invocation.EnvVars.AddRange(EnvVars());
		invocation.Arguments.AddRange(CompileArgsFor(compileUnit));
		return invocation;
	}
	
	public override IEnumerable<string> CompileArgsFor(CppCompilationUnit compileUnit)
	{
		if (compileUnit.SourceFile.ExtensionWithDot == ".asm")
		{
			yield return "/c";
			yield return compileUnit.SourceFile.InQuotes();
		}
		else
		{
			yield return compileUnit.SourceFile.InQuotes();
			foreach (var compileFlag in compileUnit.CompileFlags.Concat(DefaultCompileFlags(compileUnit)))
			{
				yield return compileFlag;
			}
			
			foreach (var define in compileUnit.Defines.Concat(ToolChainDefines()))
			{
				yield return $"/D{define}";
			}
			
			foreach (var includePath in compileUnit.IncludePaths.Concat(ToolChainIncludePaths()))
			{
				yield return $"/I\"{includePath}\"";
			}
		}
	}
	
	protected IEnumerable<string> DefaultCompileFlags(CppCompilationUnit unit)
	{
		// TODO: not support /clr
		
		yield return "/nologo";
		yield return "/c";
		yield return "/bigobj";
		yield return "/W3";
		yield return "/Z7";

	}
	
	public override IEnumerable<string> ToolChainDefines()
	{
		yield return "_WIN32";
		yield return "WIN32";
		yield return "WIN32_THREADS";
		yield return "_WINDOWS";
		yield return "WINDOWS";
		yield return "_UNICODE";
		yield return "UNICODE";
		yield return "_CRT_SECURE_NO_WARNINGS";
		yield return "_SCL_SECURE_NO_WARNINGS";
		yield return "_WINSOCK_DEPRECATED_NO_WARNINGS";
		yield return "NOMINMAX";
		
		if (Configuration == BuildConfiguration.Debug)
		{
			yield return "_DEBUG";
			yield return "DEBUG";
		}
		else
		{
			yield return "_NDEBUG";
			yield return "NDEBUG";
		}
		
		if (Arch is ARMv7Architecture)
			yield return "__arm__";
	}
	
	public override bool CanBeCompiled(NPath sourceFile)
	{
		var ex = sourceFile.ExtensionWithDot;
		return ex == ".cpp" || ex == ".cc" || ex == ".c" || ex == ".asm";
	}
	
	public override NPath CompilerExecutableFor(NPath sourceFile)
	{
		if (sourceFile.ExtensionWithDot == ".asm")
		{
			return msvcSdk.AsmCompilerPath;
		}
		else
		{
			return msvcSdk.CompilerPath;
		}
	}
}