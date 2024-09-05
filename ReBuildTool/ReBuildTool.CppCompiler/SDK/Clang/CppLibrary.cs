using NiceIO;

namespace ReBuildTool.ToolChain.SDK;

public interface ICppLibrary
{
    IEnumerable<NPath> IncludePaths();
    
    IEnumerable<NPath> LibraryPaths();
    
    IEnumerable<string> StaticLibraries();
    
    IEnumerable<string> DynamicLibraries();
}