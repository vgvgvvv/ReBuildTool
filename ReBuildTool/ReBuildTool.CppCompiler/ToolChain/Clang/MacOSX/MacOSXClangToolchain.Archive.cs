using NiceIO;

namespace ReBuildTool.ToolChain;

public partial class MacOSXClangToolchain
{
    protected override IEnumerable<string> ArchiveArgsFor(CppArchiveUnit unit)
    {
        yield return "rcs";
        
        var linkBuilder = unit.ArchiveArgsBuilder;
        
        foreach (var argument in linkBuilder.GetAllArguments())
        {
            yield return argument;
        }
        
        yield return unit.OutputPath.InQuotes();
		
        var lines = File.ReadLines(unit.ResponseFile);
        foreach (var line in lines)
        {
            yield return $"\"{line}\"";
        }
        
    }

}