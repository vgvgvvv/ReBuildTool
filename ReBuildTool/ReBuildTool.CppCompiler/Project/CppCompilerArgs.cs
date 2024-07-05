using ResetCore.Common;

namespace ReBuildTool.CppCompiler.Standalone;

public class CppCompilerArgs : CommandLineArgGroup<CppCompilerArgs>
{
	[CmdLine("root of project, work directory as default")]
	public CmdLineArg<string> ProjectRoot { get; set; } = CmdLineArg<string>.FromObject(Environment.CurrentDirectory);

	[CmdLine("dry run for test")] 
	public CmdLineArg<bool> RunDry { get; set; } = CmdLineArg<bool>.FromObject(false);
}