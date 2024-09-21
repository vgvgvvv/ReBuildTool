using NiceIO;
using ResetCore.Common;

namespace ReBuildTool.ToolChain;

public partial class GccToolChain
{
    internal override CppArchiveInvocation MakeArchiveInvocation(CppArchiveUnit cppArchiveUnit)
    {
        var invocation = new CppArchiveInvocation(cppArchiveUnit);
        invocation.ProgramName = LinuxSdk.GetArchiver();
        invocation.EnvVars.AddRange(EnvVars());
        invocation.Arguments.AddRange(ArchiveArgsFor(cppArchiveUnit));
        return invocation;
    }
	
    private IEnumerable<string> ArchiveArgsFor(CppArchiveUnit unit)
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