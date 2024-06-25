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
    public IEnumerable<string> Defines { get; set; }
    public IEnumerable<NPath> IncludePaths { get; set; }
    public IEnumerable<string> CompileFlags { get; set; }
}