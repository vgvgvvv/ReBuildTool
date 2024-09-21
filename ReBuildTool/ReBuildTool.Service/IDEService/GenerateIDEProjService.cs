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
