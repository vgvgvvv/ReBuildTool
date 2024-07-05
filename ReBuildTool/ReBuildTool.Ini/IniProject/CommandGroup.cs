using ReBuildTool.Service.Global;
using ReBuildTool.Service.CommandGroup;
using ResetCore.Common;

namespace ReBuildTool.IniProject;


public class IniProjectCommandGroup : CommandLineArgGroup<IniProjectCommandGroup>, ICommonCommandGroup
{
    [CmdLine("root of project, work directory as default")] 
    public CmdLineArg<string> ProjectRoot { get; private set; } = CmdLineArg<string>.FromObject(Environment.CurrentDirectory);

    [CmdLine("run mode: Init | Build", true)]
    public CmdLineArg<RunMode> Mode { get; private set; } = CmdLineArg<RunMode>.FromObject(RunMode.Init);

    [CmdLine("build target", true)] 
    public CmdLineArg<string> Target { get; private set; }

    [CmdLine("run dry mode, just for test")] 
    public CmdLineArg<bool> RunDry { get; private set; } = CmdLineArg<bool>.FromObject(false);

 

}