using ResetCore.Common;

namespace ReBuildTool.CSharpCompiler;

public enum CompileConfiguration
{
	Debug,
	Release
}

public class CSharpCompileArgs : CommandLineArgGroup<CSharpCompileArgs>
{
	[CmdLine("compile configuration")]
	public CompileConfiguration Configuration { get; set; } = CompileConfiguration.Debug;
}