using NiceIO;

using ReBuildTool.Service.CompileService;
using ReBuildTool.Service.Context;

using ResetCore.Common;

namespace ReBuildTool.Service.IDEService;

public enum ProjectGenType
{
	Invalid,
	VisualStudio,
	CMake,
	// Emits a .vscode folder (tasks.json / launch.json / c_cpp_properties.json) so VS Code can
	// build, run, debug and clean the project by delegating every action back to rbt.
	VSCode,
	// Emits only a compile_commands.json (JSON Compilation Database) for editor
	// code highlighting / go-to-definition, without a VS or CMake project.
	CompileCommands,
}

public class ProjectGenArgs : CommandLineArgGroup<ProjectGenArgs> 
{
	[CmdLine("gen type")]
	public CmdLineArg<ProjectGenType> IDEProjectType { get; set; }
}


public interface IGenerateIDEProjService : IProvideByService
{
	public void GenerateRuleSln(NPath projectRoot, IAssemblyCompileUnit buildRuleCompileUnit, NPath buildRuleProjectOutput);
	
	public void Generate(string name, ICppSourceProviderInterface sourceProvider, NPath projectRoot, NPath outputPath);
}
