using ReBuildTool.Service.CommandGroup;
using ResetCore.Common;

namespace ReBuildTool.CppCompiler.Standalone;

public class CppCompilerArgs : CommandLineArgGroup<CppCompilerArgs>, ICommonCommandGroup
{
	[CmdLine("root of project, work directory as default")]
	public CmdLineArg<string> ProjectRoot { get; set; } = CmdLineArg<string>.FromObject(Environment.CurrentDirectory);

	[CmdLine("run mode: Init | Build", true)]
	public CmdLineArg<RunMode> Mode { get; set; }
	
	[CmdLine("build target", true)] 
	public CmdLineArg<string> Target { get; set; }

	[CmdLine("dry run for test")] 
	public CmdLineArg<bool> RunDry { get; set; } = CmdLineArg<bool>.FromObject(false);
}