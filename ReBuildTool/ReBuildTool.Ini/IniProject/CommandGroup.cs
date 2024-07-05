using ReBuildTool.Service.Global;
using ReBuildTool.Service.CommandGroup;
using ResetCore.Common;

namespace ReBuildTool.Internal;


public class CommonCommandGroup : CommandLineArgGroup<CommonCommandGroup>, ICommonCommandGroup
{
    [CmdLine("root of project, work directory as default")] 
    public CmdLineArg<string> ProjectRoot { get; private set; } = CmdLineArg<string>.FromObject(Environment.CurrentDirectory);

    [CmdLine("run mode: Init | Build", true)]
    public CmdLineArg<RunMode> Mode { get; private set; } = CmdLineArg<RunMode>.FromObject(RunMode.Init);

    [CmdLine("build target", true)] 
    public CmdLineArg<string> Target { get; private set; }

    [CmdLine("run dry mode, just for test")] 
    public CmdLineArg<bool> RunDry { get; private set; } = CmdLineArg<bool>.FromObject(false);

    [CmdLine("debug task graph, just for test")]
    public CmdLineArg<bool> DebugTaskGraph { get; private set; }

    [CmdLine("from booster, auto update booster script")]
    public CmdLineArg<string> BoosterSource { get; private set; }

}