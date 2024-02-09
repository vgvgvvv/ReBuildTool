using ReBuildTool.Common;
using ResetCore.Common;

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
    
    [CmdLine("build target", true)]
    public string Target { get; private set; }

    [CmdLine("run dry mode, just for test")] 
    public bool RunDry { get; private set; } = false;

    [CmdLine("debug task graph, just for test")]
    public bool DebugTaskGraph { get; private set; } = false;

}