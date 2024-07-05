using ReBuildTool.Service.Global;
using ReBuildTool.Service.CommandGroup;
using ResetCore.Common;

namespace ReBuildTool.IniProject;


public class IniProjectCommandGroup : CommandLineArgGroup<IniProjectCommandGroup>, ICommonCommandGroup
{
    [CmdLine("root of project, work directory as default")] 
    public CmdLineArg<string> ProjectRoot { get; private set; } = CmdLineArg<string>.FromObject(nameof(ProjectRoot),Environment.CurrentDirectory);

    [CmdLine("run mode: Init | Build", true)]
    public CmdLineArg<RunMode> Mode { get; private set; } = CmdLineArg<RunMode>.FromObject(nameof(Mode),RunMode.Init);

    [CmdLine("build target")] 
    public CmdLineArg<string> Target { get; private set; } = CmdLineArg<string>.FromObject(nameof(ProjectRoot), Path.GetFileName(Environment.CurrentDirectory));

    [CmdLine("run dry mode, just for test")] 
    public CmdLineArg<bool> RunDry { get; private set; } = CmdLineArg<bool>.FromObject(nameof(RunDry),false);

 

}