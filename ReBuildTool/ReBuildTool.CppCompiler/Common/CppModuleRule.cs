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
    /// Every list a rule declares into, with the property name for diagnostics.
    /// A rule must fill these from <see cref="Setup"/> - see
    /// <see cref="ThrowIfDeclaredInConstructor"/>.
    /// </summary>
    private (string Name, List<string> Values)[] DeclarativeLists => new[]
    {
        (nameof(PublicIncludePaths), PublicIncludePaths),
        (nameof(PrivateIncludePaths), PrivateIncludePaths),
        (nameof(PublicDefines), PublicDefines),
        (nameof(PrivateDefines), PrivateDefines),
        (nameof(PublicCompileFlags), PublicCompileFlags),
        (nameof(PrivateCompileFlags), PrivateCompileFlags),
        (nameof(PublicLinkFlags), PublicLinkFlags),
        (nameof(PrivateLinkFlags), PrivateLinkFlags),
        (nameof(PublicArchiveFlags), PublicArchiveFlags),
        (nameof(PrivateArchiveFlags), PrivateArchiveFlags),
        (nameof(PublicStaticLibraries), PublicStaticLibraries),
        (nameof(PrivateStaticLibraries), PrivateStaticLibraries),
        (nameof(PublicDynamicLibraries), PublicDynamicLibraries),
        (nameof(PrivateDynamicLibraries), PrivateDynamicLibraries),
        (nameof(PublicLibraryDirectories), PublicLibraryDirectories),
        (nameof(PrivateLibraryDirectories), PrivateLibraryDirectories),
        (nameof(SourceDirectories), SourceDirectories),
        (nameof(SourceFiles), SourceFiles),
        (nameof(ExcludeDirectories), ExcludeDirectories),
        (nameof(ExcludeFiles), ExcludeFiles),
        (nameof(Dependencies), Dependencies),
    };

    /// <summary>
    /// Rejects a rule that declared anything from its constructor. Declarations
    /// belong in <see cref="Setup"/>: the lists are rebuilt on every setup pass
    /// (<see cref="Cleanup"/> empties them so a re-setup under a different build
    /// context does not append duplicates), so anything added once at construction
    /// time would silently disappear on the second pass. Called by the framework
    /// right after the rule is instantiated, before it injects its own paths.
    /// </summary>
    internal void ThrowIfDeclaredInConstructor()
    {
        var declared = DeclarativeLists
            .Where(list => list.Values.Count > 0)
            .Select(list => list.Name)
            .ToList();
        if (declared.Count == 0)
        {
            return;
        }

        throw new Exception(
            $"Module rule {TargetName} fills {string.Join(", ", declared)} in its constructor. " +
            $"Move those declarations into Setup(ICppBuildContext) - the rule's lists are " +
            $"rebuilt on every setup pass, so constructor-time entries do not survive.");
    }

    // Paths the framework itself contributes: the module's own Public/Private
    // directories and the generated-code directories under Intermedia. They are kept
    // apart from the lists above because they are registered once, when the rule is
    // parsed, while Cleanup() empties the live lists on every setup pass.
    private readonly List<string> FrameworkSourceDirectories = new();
    private readonly List<string> FrameworkPublicIncludePaths = new();
    private readonly List<string> FrameworkPrivateIncludePaths = new();

    internal void AddFrameworkSourceDirectory(string path)
    {
        AddFrameworkPath(FrameworkSourceDirectories, SourceDirectories, path);
    }

    internal void AddFrameworkPublicIncludePath(string path)
    {
        AddFrameworkPath(FrameworkPublicIncludePaths, PublicIncludePaths, path);
    }

    internal void AddFrameworkPrivateIncludePath(string path)
    {
        AddFrameworkPath(FrameworkPrivateIncludePaths, PrivateIncludePaths, path);
    }

    private static void AddFrameworkPath(List<string> framework, List<string> live, string path)
    {
        if (!framework.Contains(path))
        {
            framework.Add(path);
        }
        if (!live.Contains(path))
        {
            live.Add(path);
        }
    }

    /// <summary>
    /// Puts the framework-provided paths back into the live lists after a
    /// <see cref="Cleanup"/>, before the rule's own <see cref="Setup"/> runs - so a
    /// rule always sees (and can add to) them.
    /// </summary>
    private void ApplyFrameworkPaths()
    {
        foreach (var path in FrameworkSourceDirectories)
        {
            if (!SourceDirectories.Contains(path))
            {
                SourceDirectories.Add(path);
            }
        }
        foreach (var path in FrameworkPublicIncludePaths)
        {
            if (!PublicIncludePaths.Contains(path))
            {
                PublicIncludePaths.Add(path);
            }
        }
        foreach (var path in FrameworkPrivateIncludePaths)
        {
            if (!PrivateIncludePaths.Contains(path))
            {
                PrivateIncludePaths.Add(path);
            }
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
    
    /// <summary>
    /// Brings the rule up to date for <paramref name="buildContext"/>. Idempotent for
    /// a context it has already been set up with, so the repeated walks over the
    /// module graph within one operation cost nothing and cannot double-add entries.
    /// A different context (IDE generation and a build use their own
    /// <see cref="CppBuilder"/>) means the rule has to be re-declared against it:
    /// <see cref="Cleanup"/> drops what the previous pass produced, the framework
    /// paths go back in, and <see cref="Setup"/> runs again.
    /// </summary>
    public void SetupInternal(ICppBuildContext buildContext)
    {
        if (_hasSetup)
        {
            if (ReferenceEquals(BuildContext, buildContext))
            {
                return;
            }
            if (BuildContext != null)
            {
                Cleanup(BuildContext);
                ApplyFrameworkPaths();
            }
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