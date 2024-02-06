using ReBuildTool.Common;

namespace ReBuildTool.Internal;

public enum RunMode
{
    Init,
    Build
}

public class CommonCommandGroup : CommandLineArgGroup<CommonCommandGroup>
{
    [CmdLine("root of project", true)]
    public string ProjectRoot { get; }
    
    [CmdLine("run mode: Init | Build", true)]
    public RunMode Mode { get; }
    
}