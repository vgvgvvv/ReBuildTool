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
    /// Every list a rule declares into. Used to snapshot and restore the rule's
    /// declared state around a re-setup - see <see cref="CaptureDeclaredState"/>.
    /// </summary>
    private List<string>[] DeclarativeLists => new[]
    {
        PublicIncludePaths, PrivateIncludePaths,
        PublicDefines, PrivateDefines,
        PublicCompileFlags, PrivateCompileFlags,
        PublicLinkFlags, PrivateLinkFlags,
        PublicArchiveFlags, PrivateArchiveFlags,
        PublicStaticLibraries, PrivateStaticLibraries,
        PublicDynamicLibraries, PrivateDynamicLibraries,
        PublicLibraryDirectories, PrivateLibraryDirectories,
        SourceDirectories, SourceFiles,
        ExcludeDirectories, ExcludeFiles,
        Dependencies,
    };

    // The rule's state as it stood before its first Setup(): what the rule's own
    // constructor declared (dependencies, defines, ...) plus what the framework
    // injected when the rule was parsed (the module's Public/Private directories and
    // the generated-code directories). Captured once, and restored on every re-setup.
    private List<string>[]? DeclaredState;

    private void CaptureDeclaredState()
    {
        DeclaredState = DeclarativeLists.Select(list => new List<string>(list)).ToArray();
    }

    /// <summary>
    /// Puts the rule back into its declared state. <see cref="Cleanup"/> empties the
    /// lists so a second <see cref="Setup"/> doesn't append duplicates, but emptying
    /// them also drops everything the constructor and the framework put there - and a
    /// project is set up more than once per process whenever IDE project /
    /// compile_commands generation is followed by a build. Without this, that second
    /// pass reaches the compiler with no sources, no include paths and no module
    /// dependencies at all.
    /// </summary>
    private void RestoreDeclaredState()
    {
        if (DeclaredState == null)
        {
            return;
        }

        var lists = DeclarativeLists;
        for (var i = 0; i < lists.Length; i++)
        {
            lists[i].Clear();
            lists[i].AddRange(DeclaredState[i]);
        }
    }

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

    private bool _hasSetup = false;

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
    
    public ICppBuildContext? BuildContext { get; private set; }
    
    public virtual void Setup(ICppBuildContext buildContext)
    {
    }

    public virtual void Cleanup(ICppBuildContext buildContext)
    {
        PublicIncludePaths.Clear();
        PrivateIncludePaths.Clear();
        PublicDefines.Clear();
        PrivateDefines.Clear();
        PublicCompileFlags.Clear();
        PrivateCompileFlags.Clear();
        PublicLinkFlags.Clear();
        PrivateLinkFlags.Clear();
        PublicArchiveFlags.Clear();
        PrivateArchiveFlags.Clear();
        PublicStaticLibraries.Clear();
        PrivateStaticLibraries.Clear();
        PublicDynamicLibraries.Clear();
        PrivateDynamicLibraries.Clear();
        PublicLibraryDirectories.Clear();
        PrivateLibraryDirectories.Clear();
        SourceDirectories.Clear();
        SourceFiles.Clear();
        ExcludeDirectories.Clear();
        ExcludeFiles.Clear();
        Dependencies.Clear();
    }
    
    public void SetupInternal(ICppBuildContext buildContext)
    {
        if (_hasSetup && BuildContext != null)
        {
            Cleanup(BuildContext);
            RestoreDeclaredState();
        }
        else
        {
            CaptureDeclaredState();
        }
        BuildContext = buildContext;
        if (IsSupport)
        {
            Setup(BuildContext);
        }
        _hasSetup = true;
    }
    
    public virtual void PostBuild()
    {
    }

}