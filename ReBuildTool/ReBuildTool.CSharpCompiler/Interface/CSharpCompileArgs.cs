using ReBuildTool.Service.CommandGroup;
using ReBuildTool.Service.CompileService;
using ResetCore.Common;

namespace ReBuildTool.CSharpCompiler;

public class CSharpCompileArgs : CommandLineArgGroup<CSharpCompileArgs>, ICommonCommandGroup
{
	[CmdLine("root of project, work directory as default")]
	public CmdLineArg<string> ProjectRoot { get; set; } = CmdLineArg<string>.FromObject(nameof(ProjectRoot), Environment.CurrentDirectory);
	
	[CmdLine("run mode: Init | Build", true)]
	public CmdLineArg<RunMode> Mode { get; set; }

	[CmdLine("build target", true)] 
	public CmdLineArg<string> Target { get; set; }

	[CmdLine("compile configuration")]
	public CmdLineArg<CSharpCompileConfiguration> CSCompileConfig { get; set; } = CmdLineArg<CSharpCompileConfiguration>.FromObject(nameof(CSCompileConfig), CSharpCompileConfiguration.Debug);
}