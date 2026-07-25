using NiceIO;
using ReBuildTool.Service.CompileService;

namespace ReBuildTool.ToolChain;

public class CppCompilationUnit
{
    public CppCompilationUnit(IModuleInterface module)
    {
        Defines = Enumerable.Empty<string>();
        IncludePaths = Enumerable.Empty<NPath>();
        CompileFlags = Enumerable.Empty<string>();
        OwnerModule = module;
    }
    
    public NPath SourceFile { get; set; }

    /// <summary>
    /// Plain C source (.c) must not receive C++-only compiler flags (std/RTTI/exceptions),
    /// which most compilers either reject outright (gcc) or warn about as unused (clang/MSVC).
    /// </summary>
    public bool IsCFile => SourceFile.ExtensionWithDot == ".c";
    public NPath OutputFile { get; set; }
    public IEnumerable<string> Defines { get; set; }
    public IEnumerable<NPath> IncludePaths { get; set; }
    public IEnumerable<string> CompileFlags { get; set; }
    public bool OutputAssembly { get; set; } = false;

    /// <summary>
    /// When non-null, the toolchain's <c>CompileArgsFor</c> emits the flags that make the
    /// compiler write a dependency file here (GCC/Clang: <c>-MMD -MF $path</c>; MSVC:
    /// <c>/showIncludes</c>, which doesn't write a file but is parsed by ninja's
    /// <c>deps = msvc</c> trait using the <c>msvc_deps_prefix</c>). This is set ONLY by the
    /// Ninja build backend before collecting compile invocations — the direct-compile and
    /// Makefile paths leave it null so they emit no depfile/showIncludes flags (those would
    /// pollute their command lines / stdout for no benefit).
    /// <para>
    /// The value is always derived from <see cref="OutputFile"/> (e.g. <c>foo.obj</c> →
    /// <c>foo.obj.d</c>) so it lands next to the object file under ObjectCache and stays
    /// in sync across regenerations.
    /// </para>
    /// </summary>
    public NPath? DependencyFilePath { get; set; }
    
    public ICompileArgsBuilder CompileArgsBuilder { get; set; }
    public IModuleInterface OwnerModule { get; set; }
}

public class CppLinkUnit
{
    public CppLinkUnit(IModuleInterface module)
    {
        ObjectFiles = Enumerable.Empty<NPath>();
        LinkFlags = Enumerable.Empty<string>();
        LibraryPaths = Enumerable.Empty<NPath>();
        StaticLibraries = Enumerable.Empty<string>();
        DynamicLibraries = Enumerable.Empty<string>();
        OwnerModule = module;
    }
    
    public NPath OutputPath { get; set; }
    public NPath ResponseFile { get; set; }
    public IEnumerable<NPath> ObjectFiles { get; set; }
    public IEnumerable<string> LinkFlags { get; set; }
    public IEnumerable<NPath> LibraryPaths { get; set; }
    public IEnumerable<string> StaticLibraries { get; set; }
    public IEnumerable<string> DynamicLibraries { get; set; }
    public ILinkArgsBuilder LinkArgsBuilder { get; set; }
    public IModuleInterface OwnerModule { get; set; }
}

public class CppArchiveUnit
{
    public CppArchiveUnit()
    {
        StaticLibraries = Enumerable.Empty<string>();
        ArchiveFlags = Enumerable.Empty<string>();
        ObjectFiles = Enumerable.Empty<NPath>();
    }
    
    public NPath OutputPath { get; set; }
    public NPath ResponseFile { get; set; }
    public IEnumerable<NPath> LibraryPaths { get; set; }
    public IEnumerable<string> StaticLibraries { get; set; }
    public IEnumerable<string> ArchiveFlags { get; set; }
    public IEnumerable<NPath> ObjectFiles { get; set; }
    public IArchiveArgsBuilder ArchiveArgsBuilder { get; set; }
}