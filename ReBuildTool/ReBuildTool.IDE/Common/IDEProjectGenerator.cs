using NiceIO;

using ReBuildTool.IDE.CompileCommands;
using ReBuildTool.Service.CompileService;
using ReBuildTool.Service.Context;
using ReBuildTool.Service.Global;
using ReBuildTool.Service.IDEService;
using ReBuildTool.Service.IDEService.CMake;
using ReBuildTool.Service.IDEService.VisualStudio;
using ReBuildTool.Service.IDEService.VSCode;

namespace ReBuildTool.IDE.Common;

public class IDEProjectGenerator : IGenerateIDEProjService
{
	public void GenerateRuleSln(NPath projectRoot, IAssemblyCompileUnit buildRuleCompileUnit, NPath buildRuleProjectOutput)
	{
		var slnResult = ServiceContext.Instance.Create<ISlnGenerator>("BuildRule", projectRoot);
		if (!slnResult)
		{
			throw new Exception("cannot parse sln generator");
		}
		
		var slnGenerator = slnResult.Value;
		var compiler = ServiceContext.Instance.FindService<ICSharpCompilerService>().Value;
		slnGenerator.GenerateOrGetNetCoreCSProj(buildRuleCompileUnit, compiler.DefaultEnvironment,
			buildRuleProjectOutput);
		slnGenerator.FlushProjectsAndSln();
	}
	
	public void Generate(string name, ICppSourceProviderInterface sourceProvider, NPath projectRoot, NPath outputPath)
	{
		var finalProjectType = ProjectGenArgs.Get().IDEProjectType.Value;
		if (finalProjectType == ProjectGenType.Invalid)
		{
			if (PlatformHelper.IsWindows())
			{
				finalProjectType = ProjectGenType.VisualStudio;
			}
			else if (PlatformHelper.IsLinux() || PlatformHelper.IsOSX())
			{
				finalProjectType = ProjectGenType.CMake;
			}
		}
		
		if (finalProjectType == ProjectGenType.VisualStudio)
		{
			var slnResult = ServiceContext.Instance.Create<ISlnGenerator>(name, projectRoot);
			if (!slnResult)
			{
				throw new Exception("cannot parse sln generator");
			}
		
			var slnGenerator = slnResult.Value;
			slnGenerator.GenerateOrGetVCProj(sourceProvider, outputPath.Combine("VCProjects"));
			slnGenerator.FlushProjectsAndSln();
		}
		else if (finalProjectType == ProjectGenType.CMake)
		{
			var cmakeResult = ServiceContext.Instance.Create<ICMakeGenerator>(name, projectRoot);
			if (!cmakeResult)
			{
				throw new Exception("cannot parse cmake generator");
			}

			var cmakeProject = cmakeResult.Value;
			cmakeProject.GenerateCMakeProject(sourceProvider, outputPath.Combine ("CMakeProjects"));

			cmakeProject.FlushAllCMakeFile();
		}
		else if (finalProjectType == ProjectGenType.VSCode)
		{
			// The .vscode folder must live at the workspace root for VS Code to pick it up, so it
			// is written under projectRoot rather than the intermediate output path.
			var vscodeResult = ServiceContext.Instance.Create<IVSCodeGenerator>(name, projectRoot);
			if (!vscodeResult)
			{
				throw new Exception("cannot parse vscode generator");
			}

			var vscodeProject = vscodeResult.Value;
			vscodeProject.GenerateVSCodeProject(sourceProvider, projectRoot);
			vscodeProject.FlushAllFiles();
		}

		// A compile_commands.json is emitted for every project type (and is the sole output when
		// CompileCommands is selected on its own), so editors get code highlighting / navigation
		// regardless of which IDE project was generated.
		CompileCommandsGenerator.Generate(sourceProvider);
	}
	

}
