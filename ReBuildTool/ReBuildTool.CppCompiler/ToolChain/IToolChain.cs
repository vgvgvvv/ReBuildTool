using NiceIO;

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
}

internal class InvocationBase
{
	public bool Run()
	{
		bool isSucc = false;
		SimpleExec.Command.Run(ProgramName, Arguments.ToArray(), handleExitCode: (code) =>
		{
			isSucc = code != 0;
			return isSucc;
		});
		return isSucc;
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