using System.Diagnostics;
using NiceIO;
using ReBuildTool.Service.Global;
using ResetCore.Common;

namespace ReBuildTool.ToolChain;


public abstract class IToolChain
{
	public BuildConfiguration Configuration { get; private set; }
	public Architecture Arch { get; private set; }
	
	protected IToolChain(BuildConfiguration configuration, Architecture arch)
	{
		Configuration = configuration;
		Arch = arch;
	}
	
	public abstract IEnumerable<string> ToolChainDefines();

	public abstract Dictionary<string, string> EnvVars();

	public abstract IEnumerable<NPath> ToolChainIncludePaths();

	public abstract IEnumerable<NPath> ToolChainLibraryPaths();

	public abstract IEnumerable<NPath> ToolChainStaticLibraries();
	
	public abstract IEnumerable<NPath> ToolChainDynamicLibraries();

	public abstract bool CanBeCompiled(NPath sourceFile);

	public abstract string ObjectExtension { get; }
	
	public abstract string ExecutableExtension { get; }
	
	public abstract string StaticLibraryExtension { get; }
	
	public abstract string DynamicLibraryExtension { get; }

	public abstract NPath CompilerExecutableFor(NPath sourceFile);
	
	public abstract IEnumerable<string> CompileArgsFor(CppCompilationUnit cppCompilationInstruction);
	
	internal abstract CppCompileInvocation MakeCompileInvocation(CppCompilationUnit compileUnit);
	
	internal abstract CppLinkInvocation MakeLinkInvocation(CppLinkUnit cppLinkUnit);
	
	internal abstract CppArchiveInvocation MakeArchiveInvocation(CppArchiveUnit cppArchiveUnit);
	
	public abstract ICompileArgsBuilder MakeCompileArgsBuilder();
	
	public abstract ILinkArgsBuilder MakeLinkArgsBuilder();
	
	public abstract IArchiveArgsBuilder MakeArchiveArgsBuilder();
	
}

internal class InvocationBase
{
	public bool Run()
	{
		using (var shell = Shell.Create())
		{
			shell.WithProgram(ProgramName)
				.WithArguments(Arguments)
				.WithEnvVars(EnvVars)
				.Execute()
				.WaitForEnd();

			return shell.IsSuccess();
		}
		
	}
		
	public string ProgramName { get; set; }
	public  List<string> Arguments { get; } = new();
	public  Dictionary<string, string> EnvVars { get; } = new();

	public override string ToString()
	{
		return $"{ProgramName} {string.Join(' ', Arguments)}";
	}
}

internal class CppCompileInvocation : InvocationBase { }

internal class CppLinkInvocation : InvocationBase { }

internal class CppArchiveInvocation : InvocationBase { }

public class IArgsBuilder
{
	public void Append(string arg)
	{
		Arguments.Add(arg);
	}
	
	public virtual IEnumerable<string> GetAllArguments()
	{
		return Arguments;
	}
	
	private List<string> Arguments { get; } = new();
}

public abstract class ICompileArgsBuilder : IArgsBuilder
{
	public abstract void DisableException(bool enable);

	public abstract void DisableWarnings(string warnCode);
	
	public abstract void SetWarnAsError(bool enable);
	
	public abstract void SetLto(bool enable);
}

public abstract class ILinkArgsBuilder : IArgsBuilder
{
	public abstract void DisableWarnings(string warnCode);
	
	public abstract void SetLto(bool enable);
	
	public abstract void SetFastLink(bool enable);

	public abstract void SetWarnAsError(bool enable);
}

public abstract class IArchiveArgsBuilder : IArgsBuilder
{
	public abstract void SetLto(bool enable);
}