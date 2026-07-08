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

    /// <summary>
    /// Explicit individual source files to compile, in addition to the
    /// <see cref="SourceDirectories"/> globs. Entries are resolved relative to
    /// <see cref="ModuleDirectory"/> when not absolute. Useful for libraries
    /// whose build needs a precise file list that a directory glob can't
    /// express — e.g. FreeType's aggregate .c (one per module that #includes
    /// its siblings) or Assimp's per-format importer sources.
    /// </summary>
    public List<string> SourceFiles { get; } = new();

    /// <summary>
    /// Directories excluded from the recursive <see cref="SourceDirectories"/>
    /// glob. A file is dropped if its path is under any of these dirs.
    /// Entries are resolved relative to <see cref="ModuleDirectory"/> when not
    /// absolute. Useful for keeping platform-specific subdirs (e.g. mimalloc's
    /// src/prim/{windows,osx,unix,wasi}/) off the compile glob.
    /// </summary>
    public List<string> ExcludeDirectories { get; } = new();

    /// <summary>
    /// Specific files excluded from both the <see cref="SourceDirectories"/>
    /// glob and <see cref="SourceFiles"/>. Entries are resolved relative to
    /// <see cref="ModuleDirectory"/> when not absolute. Useful for per-platform
    /// source filtering (e.g. skip glfw's cocoa_time.c / x11_*.c off-target).
    /// </summary>
    public List<string> ExcludeFiles { get; } = new();

    public List<string> Dependencies { get; } = new();

    public string ModuleDirectory { get; internal set; }

    /// <summary>
    /// Resolve a path entry from <see cref="SourceFiles"/> /
    /// <see cref="ExcludeDirectories"/> / <see cref="ExcludeFiles"/>: absolute
    /// paths are kept as-is; relative paths are combined with
    /// <see cref="ModuleDirectory"/>. Returns the input unchanged if
    /// <see cref="ModuleDirectory"/> is not yet set.
    /// </summary>
    internal string ResolveSourcePath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }
        if (System.IO.Path.IsPathRooted(path))
        {
            return path;
        }
        return string.IsNullOrEmpty(ModuleDirectory)
            ? path
            : System.IO.Path.Combine(ModuleDirectory, path);
    }

    public virtual bool IsSupport { get; } = true;

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
    
    public virtual void Setup(ICppBuildContext buildContext)
    {
    }
    
    public void SetupInternal(ICppBuildContext buildContext)
    {
        BuildContext = buildContext;
        if (IsSupport)
        {
            Setup(BuildContext);
        }
    }
    
    public virtual void PostBuild()
    {
    }

}