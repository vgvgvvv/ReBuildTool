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
	public string CSharpBuildRoot { get; set; } = Environment.CurrentDirectory;
	
	[CmdLine("compile configuration")]
	public CompileConfiguration CSCompileConfig { get; set; } = CompileConfiguration.Debug;
}