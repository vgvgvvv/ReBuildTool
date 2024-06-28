using NiceIO;
using ReBuildTool.Common;
using ReBuildTool.Service.CompileService;
using ReBuildTool.Service.Context;
using ReBuildTool.Service.IDEService.VisualStudio;

namespace ReBuildTool.IDE.VisualStudio;


public class SlnGenerator : ISlnGenerator
{
	
	public SlnGenerator(string name, NPath outputPath)
	{
		Name = name;
		outputFolder = outputPath;
		outputFolder.EnsureDirectoryExists();
	}

	public ISlnSubProject GenerateOrGetNetCoreCSProj(IAssemblyCompileUnit unit,
		ICSharpCompileEnvironment env, NPath output)
	{
		return NetCoreCSProj.GenerateOrGetCSProj(this, unit, env, outputFolder);
	}

	public ISlnSubProject GenerateOrGetNetFrameworkCSProj(IAssemblyCompileUnit unit,
		ICSharpCompileEnvironment env, NPath output)
	{
		return NetFrameworkCSProj.GenerateOrGetCSProj(this, unit, env, output);
	}
	
	public ISlnSubProject GenerateOrGetVCProj(ICppSourceProviderInterface source, NPath output)
	{
		return VCProject.GenerateOrGetVCProj(this, source, output);
	}

	public void RegisterProj(ISlnSubProject proj)
	{
		if (!NetFrameworkCSProjs.TryGetValue(proj.guid, out var outProj))
		{
			NetFrameworkCSProjsByName.Add(proj.name, proj);
			NetFrameworkCSProjs.Add(proj.guid, proj);
		}
	}

	public bool GetSubProj(string name, out ISlnSubProject? outProj)
	{
		return NetFrameworkCSProjsByName.TryGetValue(name, out outProj);
	}

	public bool FlushProjectsAndSln()
	{
		foreach (var (key, proj) in NetFrameworkCSProjsByName)
		{
			proj.FlushToFile();
		}

		FlushSln();
		return true;
	}

	private void FlushSln()
	{
		codeBuilder.AppendLine("Microsoft Visual Studio Solution File, Format Version 11.00");
		codeBuilder.AppendLine("# Visual Studio 2010");
		foreach (var (key, proj) in NetFrameworkCSProjsByName)
		{
			codeBuilder.AppendLine(
				$"Project(\"{{{proj.guid}}}\") = \"{proj.name}\", \"{proj.fullPath}\", \"{{{proj.guid}}}\"");
			codeBuilder.AppendLine("EndProject");
		}

		codeBuilder.AppendLine("Global");
		{
			codeBuilder.AddIndent();

			codeBuilder.AppendLine("GlobalSection(SolutionConfigurationPlatforms) = preSolution");
			{
				codeBuilder.AddIndent();

				codeBuilder.AppendLine("Debug|Any CPU = Debug|Any CPU");
				codeBuilder.AppendLine("Release|Any CPU = Release|Any CPU");
				
				codeBuilder.RemoveIndent();
			}
			codeBuilder.AppendLine("EndGlobalSection");
			
			codeBuilder.AppendLine("GlobalSection(ProjectConfigurationPlatforms) = postSolution");
			{
				codeBuilder.AddIndent();

				foreach (var (key, proj) in NetFrameworkCSProjsByName)
				{
					codeBuilder.AppendLine($"{{{proj.guid}}}.Debug|Any CPU.ActiveCfg = Debug|Any CPU");
					codeBuilder.AppendLine($"{{{proj.guid}}}.Debug|Any CPU.Build.0 = Debug|Any CPU");
					codeBuilder.AppendLine($"{{{proj.guid}}}.Release|Any CPU.ActiveCfg = Release|Any CPU");
					codeBuilder.AppendLine($"{{{proj.guid}}}.Release|Any CPU.Build.0 = Release|Any CPU");
				}
				
				codeBuilder.RemoveIndent();
			}
			codeBuilder.AppendLine("EndGlobalSection");
				
			codeBuilder.AppendLine("GlobalSection(SolutionProperties) = preSolution");
			{
				codeBuilder.AddIndent();
				codeBuilder.AppendLine("HideSolutionNode = FALSE");
				codeBuilder.RemoveIndent();
			}
			codeBuilder.AppendLine("EndGlobalSection");
			
			
			codeBuilder.RemoveIndent();
		}
		codeBuilder.AppendLine("EndGlobal");

		File.WriteAllText(OutputPath, codeBuilder.ToString());
	}

	public string Name { get; }
	
	public string OutputPath => outputFolder.Combine(Name + ".sln");
	
	public Dictionary<string, ISlnSubProject> NetFrameworkCSProjsByName { get; } =
		new Dictionary<string, ISlnSubProject>();
	public Dictionary<Guid, ISlnSubProject> NetFrameworkCSProjs { get; } = new Dictionary<Guid, ISlnSubProject>();

	private NPath outputFolder;
	private SourceCodeBuilder codeBuilder = new SourceCodeBuilder();
	
}