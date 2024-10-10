using NiceIO;
using ReBuildTool.ToolChain.SDK;
using ResetCore.Common;

namespace ReBuildTool.ToolChain;

public partial class MSVCToolChain 
{
	internal override CppCompileInvocation MakeCompileInvocation(CppCompilationUnit compileUnit)
	{
		var invocation = new CppCompileInvocation(compileUnit);
		invocation.ProgramName = CompilerExecutableFor(compileUnit.SourceFile);
		invocation.EnvVars.AddRange(EnvVars());
		invocation.Arguments.AddRange(CompileArgsFor(compileUnit));
		return invocation;
	}
	
	public override IEnumerable<string> CompileArgsFor(CppCompilationUnit compileUnit)
	{
		bool isAsm = compileUnit.SourceFile.ExtensionWithDot == ".asm";
		if (isAsm)
		{
			yield return "/c";
		}
		else
		{
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
			
			yield return "/Fd" + compileUnit.OutputFile.ChangeExtension(".pdb").InQuotes();
			if (compileUnit.OutputAssembly)
			{
				yield return "/Fa" + compileUnit.SourceFile.ChangeExtension(".s").InQuotes();
			}
		}

		yield return "/Fo" + compileUnit.OutputFile.InQuotes();
		
		yield return compileUnit.SourceFile.InQuotes();

	}
	
	protected IEnumerable<string> DefaultCompileFlags(CppCompilationUnit unit)
	{
		// TODO: not support /clr
		// yield return "/clr";
		
		yield return "/nologo";
		yield return "/c";
		yield return "/bigobj";
		yield return "/W3";
		yield return "/Z7";
		
		// Always /Gy to avoid ARM linker failure:
		// "fatal error LNK1322: cannot avoid potential ARM hazard (QSD8960 P1 processor bug) in section 4; please consider using compiler option /Gy if it was not used"
		// See case 766755
		yield return "/Gy";

		yield return "/MP"; // Multi-processor compilation
		
		yield return "/utf-8";
		
		yield return "/Zc:inline"; // enable /Zc:inline by default to speed up linkage progress

		if (Configuration == BuildConfiguration.Debug)
		{
			yield return "/Od";
			yield return "/MTd"; // default link static CRT
			// '/RTC1' and '/clr' command-line options are incompatible
			// if (!hasClrFlag && !DontLinkCrt)
			// 	yield return "/RTC1"; // runtime errror check
			//
			// if (DontLinkCrt)
			// 	yield return "/GS-"; // Buffer Security Check
		}
		else
		{
			if (Configuration == BuildConfiguration.ReleaseSize)
				yield return "/O1"; //MinimizeSize
			else
				yield return "/Ox"; // Enable Most Speed Optimizations

			yield return "/Oi"; // Generate Intrinsic Functions
			// we recommend that you specify the /Oy- option after any other optimization compiler options.
			yield return "/Oy-"; // Frame-Pointer Omission 
			yield return "/GS-"; // Buffer Security Check, 
			yield return "/Gw"; // Optimize Global Data
			yield return "/GF"; // Eliminate Duplicate Strings
			// Generate enhanced debugging information for optimized code in non-debug builds.
			yield return "/Zo"; // Enhance Optimized Debugging
			
			yield return "/MT"; // default link static CRT
		}
		
		foreach (var argument in unit.CompileArgsBuilder.GetAllArguments())
		{
			yield return argument;
		}
	}
	
	public override IEnumerable<string> ToolChainDefines()
	{
		yield return "COMPILER_MSVC";
		foreach (var define in MSVCConst.DefaultDefines(Configuration, Arch))
		{
			yield return define;
		}
	}
	
	public override bool CanBeCompiled(NPath sourceFile)
	{
		var ex = sourceFile.ExtensionWithDot;
		return ex == ".cpp" || ex == ".cc" || ex == ".c" || ex == ".asm" || ex == ".inl";
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