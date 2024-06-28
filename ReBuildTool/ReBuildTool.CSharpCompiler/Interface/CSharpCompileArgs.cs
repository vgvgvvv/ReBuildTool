using ReBuildTool.Service.CompileService;
using ResetCore.Common;

namespace ReBuildTool.CSharpCompiler;

public class CSharpCompileArgs : CommandLineArgGroup<CSharpCompileArgs>
{
	[CmdLine("root of project, work directory as default")]
	public string CSharpBuildRoot { get; set; } = Environment.CurrentDirectory;
	
	[CmdLine("compile configuration")]
	public CSharpCompileConfiguration CSCompileConfig { get; set; } = CSharpCompileConfiguration.Debug;
}