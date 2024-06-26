using ResetCore.Common;

namespace ReBuildTool.CppCompiler.Standalone;

public class CppCompilerArgs : CommandLineArgGroup<CppCompilerArgs>
{
	[CmdLine("root of project, work directory as default")]
	public string CppBuildRoot { get; set; } = Environment.CurrentDirectory;

	[CmdLine("dry run for test")] 
	public bool DryRun { get; set; } = false;
}