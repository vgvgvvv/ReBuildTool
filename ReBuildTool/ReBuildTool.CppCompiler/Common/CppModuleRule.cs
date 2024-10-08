using ReBuildTool.Service.CompileService;

namespace ReBuildTool.ToolChain;


public abstract partial class CppModuleRule : IModuleInterface, IPostBuildModule
{
    public virtual BuildType TargetBuildType { get; set; } = BuildType.DynamicLibrary;
    
    public virtual string TargetName => GetType().Name;
    
    public List<string> PublicIncludePaths { get; } = new();

    public List<string> PrivateIncludePaths { get; } = new();

    public List<string> PublicDefines { get; } = new();

    public List<string> PrivateDefines { get; } = new();

    public List<string> PublicCompileFlags { get; } = new();

    public List<string> PrivateCompileFlags { get; } = new();

    public List<string> PublicLinkFlags { get; } = new();

    public List<string> PrivateLinkFlags { get; } = new();
    
    public List<string> PublicArchiveFlags { get; } = new();

    public List<string> PrivateArchiveFlags { get; } = new();

    public List<string> PublicStaticLibraries { get; } = new();
    
    public List<string> PrivateStaticLibraries { get; } = new();
    
    public List<string> PublicDynamicLibraries { get; } = new();
    
    public List<string> PrivateDynamicLibraries { get; } = new();
    
    public List<string> PublicLibraryDirectories { get; } = new();
    
    public List<string> PrivateLibraryDirectories { get; } = new();
    
    public List<string> SourceDirectories { get; } = new();

    public List<string> Dependencies { get; } = new();
    
    public string ModuleDirectory { get; internal set; }
    
    public virtual IEnumerable<string> CompileFlagsFor(CppCompilationUnit compilationUnit)
    {
        return Enumerable.Empty<string>();
    }
    
    public virtual IEnumerable<string> DefinesFor(CppCompilationUnit compilationUnit)
    {
        return Enumerable.Empty<string>();
    }
    
    public virtual IEnumerable<string> IncludePathsFor(CppCompilationUnit compilationUnit)
    {
        return Enumerable.Empty<string>();
    }

    public virtual void AdditionCompileArgs(ICompileArgsBuilder builder)
    {
        
    }
    
    public virtual void AdditionLinkArgs(ILinkArgsBuilder builder)
    {
        
    }
    
    public virtual void AdditionArchiveArgs(IArchiveArgsBuilder builder)
    {
        
    }
    
    public ICppBuildContext BuildContext { get; private set; }
    
    public void Setup(ICppBuildContext buildContext)
    {
        BuildContext = buildContext;
    }
    
    public virtual void PostBuild()
    {
    }

}