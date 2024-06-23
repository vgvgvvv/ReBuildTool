using ResetCore.Common;

namespace ReBuildTool.CSharpCompiler;

public enum CompileConfiguration
{
	Debug,
	Release
}

public class CSharpCompileArgs : CommandLineArgGroup<CSharpCompileArgs>
{
	[CmdLine("root of project, work directory as default")]
	public string ProjectRoot { get; set; } = Environment.CurrentDirectory;
	
	[CmdLine("compile configuration")]
	public CompileConfiguration Configuration { get; set; } = CompileConfiguration.Debug;
}