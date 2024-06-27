namespace ReBuildTool.ToolChain;

public enum BuildType
{
    StaticLibrary,
    DynamicLibrary,
    Executable
}

public abstract class ModuleRule
{
    public BuildType BuildType { get; set; } = BuildType.DynamicLibrary;
    
    public string TargetName => GetType().Name;
    
    public List<string> PublicIncludePaths { get; } = new();

    public List<string> PrivateIncludePaths { get; } = new();

    public List<string> PublicDefines { get; } = new();

    public List<string> PrivateDefines { get; } = new();

    public List<string> PublicCompilerFlags { get; } = new();

    public List<string> PrivateCompileFlags { get; } = new();

    public List<string> PublicLinkFlags { get; } = new();

    public List<string> PrivateLinkFlags { get; } = new();

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

}