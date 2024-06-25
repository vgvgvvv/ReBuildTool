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

	public abstract IEnumerable<string> ObjectOutputArguments(NPath objectFile, NPath sourceFile);
	
	public abstract IEnumerable<string> ToolChainDefines();

	public abstract IEnumerable<NPath> EnvVars();

	public abstract IEnumerable<NPath> ToolChainIncludePaths();

	public abstract IEnumerable<NPath> ToolChainLibraryPaths();

	public abstract IEnumerable<NPath> ToolChainStaticLibraries();

	public abstract bool CanBeCompiled(NPath sourceFile);

	public abstract string ObjectExtension { get; }
	
	public abstract string ExecutableExtension { get; }
	
	public abstract string StaticLibraryExtension { get; }
	
	public abstract string DynamicLibraryExtension { get; }

	public abstract NPath CompilerExecutableFor(NPath sourceFile);

	public abstract IEnumerable<string> CompilerFlagsFor(CppCompilationUnit cppCompilationInstruction);


}