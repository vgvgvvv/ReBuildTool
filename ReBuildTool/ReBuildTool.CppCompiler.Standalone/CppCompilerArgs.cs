using ResetCore.Common;

namespace ReBuildTool.CppCompiler.Standalone;

public class CppCompilerArgs : CommandLineArgGroup<CppCompilerArgs>
{
	[CmdLine("root of project, work directory as default")]
	public string ProjectRoot { get; set; } = Environment.CurrentDirectory;
}