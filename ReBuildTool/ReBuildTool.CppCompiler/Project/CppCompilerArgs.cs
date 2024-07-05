using NiceIO;
using ReBuildTool.Service.CommandGroup;
using ResetCore.Common;

namespace ReBuildTool.CppCompiler.Standalone;

public class CppCompilerArgs : CommandLineArgGroup<CppCompilerArgs>, ICommonCommandGroup
{
	[CmdLine("root of project, work directory as default")]
	public CmdLineArg<string> ProjectRoot { get; set; } = CmdLineArg<string>.FromObject(nameof(ProjectRoot), Environment.CurrentDirectory);

	[CmdLine("run mode: Init | Build", true)]
	public CmdLineArg<RunMode> Mode { get; set; }
	
	[CmdLine("build target")] 
	public CmdLineArg<string> Target { get; set; } = CmdLineArg<string>.FromObject(nameof(ProjectRoot), Path.GetFileName(Environment.CurrentDirectory));

	[CmdLine("dry run for test")] 
	public CmdLineArg<bool> RunDry { get; set; } = CmdLineArg<bool>.FromObject(nameof(RunDry), false);
}