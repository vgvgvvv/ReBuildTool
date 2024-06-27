using NiceIO;

namespace ReBuildTool.ToolChain;

public class BuildOptions
{
	public Architecture Architecture = new x64Architecture();
	public BuildConfiguration Configuration = BuildConfiguration.Debug;
	
	public List<NPath> CustomIncludeDirectories { get; } = new List<NPath>();
	
	public List<string> CustomDefines { get; } = new List<string>();
	
	public List<string> CustomCompileFlags { get; } = new List<string>();

	public List<string> CustomLinkFlags { get; } = new List<string>();
	
	public List<string> CustomStaticLibraries { get; } = new List<string>();
	
	public List<string> CustomDynamicLibraries { get; } = new List<string>();
	
	public List<NPath> CustomLibraryDirectories { get; } = new List<NPath>();

	public List<string> CustomArchiveFlags { get; } = new List<string>();
}