using NiceIO;
using ReBuildTool.Service.Context;

namespace ReBuildTool.Service.CompileService;

public enum BuildType
{
	StaticLibrary,
	DynamicLibrary,
	Executable
}



public interface IModuleInterface
{
	public BuildType BuildType { get; }
    
	public string TargetName => GetType().Name;
    
	public List<string> PublicIncludePaths { get; } 

	public List<string> PrivateIncludePaths { get; } 

	public List<string> PublicDefines { get; } 

	public List<string> PrivateDefines { get; }

	public List<string> PublicCompilerFlags { get; }

	public List<string> PrivateCompileFlags { get; } 

	public List<string> PublicLinkFlags { get; } 

	public List<string> PrivateLinkFlags { get; } 
    
	public List<string> PublicArchiveFlags { get; } 

	public List<string> PrivateArchiveFlags { get; } 

	public List<string> PublicStaticLibraries { get; } 
    
	public List<string> PrivateStaticLibraries { get; } 
    
	public List<string> PublicDynamicLibraries { get; } 
    
	public List<string> PrivateDynamicLibraries { get; } 
    
	public List<string> PublicLibraryDirectories { get; } 
    
	public List<string> PrivateLibraryDirectories { get; } 
    
	public List<string> SourceDirectories { get; } 

	public List<string> Dependencies { get; } 
    
	public string ModuleDirectory { get; }
	
}

public interface ITargetInterface
{
	public List<string> UsedModules { get; }
    
	public string TargetDirectory { get; }
}

public interface ICppSourceProviderInterface
{
	string Name { get; }
	NPath ProjectRoot { get; }
	NPath SourceFolder { get; }
	NPath IntermediaFolder { get; }
	Dictionary<string, ITargetInterface> TargetRules { get; }
	Dictionary<string, IModuleInterface> ModuleRules { get; }
}

public interface ICppProject : IProvideByService
{
	public const string TargetDefineExtension = ".Target.cs";
	public const string ModuleDefineExtension = ".Module.cs";
	public const string ExtensionDefineExtension = ".Extension.cs";
	
	void Parse();
	void Setup();
	void Build(string? targetName = null);
	void Clean();
	void ReBuild(string? targetName = null);
}