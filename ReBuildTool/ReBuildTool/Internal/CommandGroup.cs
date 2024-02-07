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
    public string ProjectRoot { get; private set; }
    
    [CmdLine("run mode: Init | Build", true)]
    public RunMode Mode { get; private set; }
    
}