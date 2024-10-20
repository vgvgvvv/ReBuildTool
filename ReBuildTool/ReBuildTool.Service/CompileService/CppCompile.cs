using NiceIO;
using ReBuildTool.Service.Context;

namespace ReBuildTool.Service.CompileService;

public enum BuildType
{
	StaticLibrary,
	DynamicLibrary,
	Executable
}

public interface IPostBuildModule
{
	public void PostBuild();
}

public interface IObjectiveCModule
{
	public List<string> Frameworks { get; }
}

public interface IModuleInterface
{
	public BuildType TargetBuildType { get; }

	public string TargetName { get; }
    
	public List<string> PublicIncludePaths { get; } 

	public List<string> PrivateIncludePaths { get; } 

	public List<string> PublicDefines { get; } 

	public List<string> PrivateDefines { get; }

	public List<string> PublicCompileFlags { get; }

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
	
	public bool IsSupport { get; }
	
}

public interface IPostBuildTarget
{
	public void PostBuild();
}

public interface ITargetCompilePlugin
{

	public void Setup(ICppSourceProviderInterface sourceProvider);

}

public class GitLibrary
{
	public string Name { get; set; }
	public string Url { get; set; }
	public string Branch { get; set; }
}

public interface ITargetInterface
{
	public List<string> UsedModules { get; }
    
	public string TargetDirectory { get; }
	
	public List<ITargetCompilePlugin> Plugins { get; }
	
	public Dictionary<string, string> CustomInfo { get; }
	
	public List<GitLibrary> GitLibraries { get; }
}

public interface ICppSourceProviderInterface
{
	string Name { get; }
	NPath ProjectRoot { get; }
	NPath OutputRoot { get; }
	NPath SourceFolder { get; }
	NPath IntermediaFolder { get; }
	Dictionary<string, ITargetInterface> TargetRules { get; }
	Dictionary<string, IModuleInterface> ModuleRules { get; }
}

public interface ICppProject : IProjectInterface
{
	public const string TargetDefineExtension = ".target.cs";
	public const string ModuleDefineExtension = ".module.cs";
	public const string ExtensionDefineExtension = ".extension.cs";
}

