using NiceIO;

namespace ReBuildTool.ToolChain;

public class CppCompilationUnit
{
    public CppCompilationUnit()
    {
        Defines = Enumerable.Empty<string>();
        IncludePaths = Enumerable.Empty<NPath>();
        LumpPaths = Enumerable.Empty<NPath>();
        CompilerFlags = Enumerable.Empty<string>();
        TreatWarningsAsErrors = true;
    }
    
    public NPath SourceFile { get; set; }
    public IEnumerable<string> Defines { get; set; }
    public IEnumerable<NPath> IncludePaths { get; set; }
    public IEnumerable<NPath> LumpPaths { get; set; }
    public IEnumerable<string> CompilerFlags { get; set; }
    public NPath CacheDirectory { get; set; }
    public bool TreatWarningsAsErrors { get; set; }
}