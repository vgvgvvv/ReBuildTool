using NiceIO;

namespace ReBuildTool.ToolChain;

public class CppCompilationUnit
{
    public CppCompilationUnit()
    {
        Defines = Enumerable.Empty<string>();
        IncludePaths = Enumerable.Empty<NPath>();
        CompileFlags = Enumerable.Empty<string>();
    }
    
    public NPath SourceFile { get; set; }
    public NPath OutputFile { get; set; }
    public IEnumerable<string> Defines { get; set; }
    public IEnumerable<NPath> IncludePaths { get; set; }
    public IEnumerable<string> CompileFlags { get; set; }
}

public class CppLinkUnit
{
    public CppLinkUnit()
    {
        ObjectFiles = Enumerable.Empty<NPath>();
        LinkFlags = Enumerable.Empty<string>();
        LibraryPaths = Enumerable.Empty<NPath>();
        StaticLibraries = Enumerable.Empty<string>();
        DynamicLibraries = Enumerable.Empty<string>();
    }
    
    public NPath OutputPath { get; set; }
    public IEnumerable<NPath> ObjectFiles { get; set; }
    public IEnumerable<string> LinkFlags { get; set; }
    public IEnumerable<NPath> LibraryPaths { get; set; }
    public IEnumerable<string> StaticLibraries { get; set; }
    public IEnumerable<string> DynamicLibraries { get; set; }
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
    public IEnumerable<NPath> LibraryPaths { get; set; }
    public IEnumerable<string> StaticLibraries { get; set; }
    public IEnumerable<string> ArchiveFlags { get; set; }
    public IEnumerable<NPath> ObjectFiles { get; set; }
}