namespace ReBuildTool.ToolChain;

public abstract class ModuleRule
{
    public string TargetName => GetType().Name;
    
    public List<string> PublicIncludePaths { get; } = new();

    public List<string> PrivateIncludePaths { get; } = new();

    public List<string> PublicDefines { get; } = new();

    public List<string> PrivateDefines { get; } = new();

    public List<string> PublicCompilerFlags { get; } = new();

    public List<string> PrivateCompileFlags { get; } = new();

    public List<string> PublicLinkFlags { get; } = new();

    public List<string> PrivateLinkFlags { get; } = new();

    public List<string> PublicLibraries { get; } = new();
    
    public List<string> PrivateLibraries { get; } = new();
    
    public List<string> PublicLibraryDirectories { get; } = new();
    
    public List<string> PrivateLibraryDirectories { get; } = new();
    
    public List<string> SourceDirectories { get; } = new();

    public List<string> Dependencies { get; } = new();
    
    public string ModuleDirectory { get; internal set; }

    public virtual IEnumerable<string> CompileFlagsFor(CppCompilationUnit compilationUnit)
    {
        return Enumerable.Empty<string>();
    }

}