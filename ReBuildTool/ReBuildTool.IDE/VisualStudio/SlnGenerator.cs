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
		return NetCoreCSProj.GenerateOrGetCSProj(this, unit, env, output);
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
		if (!SubProjects.TryGetValue(proj.guid, out var outProj))
		{
			SubProjectsByName.Add(proj.name, proj);
			SubProjects.Add(proj.guid, proj);
		}
	}

	public bool GetSubProj(string name, out ISlnSubProject? outProj)
	{
		return SubProjectsByName.TryGetValue(name, out outProj);
	}

	public bool FlushProjectsAndSln()
	{
		foreach (var (key, proj) in SubProjectsByName)
		{
			proj.FlushToFile();
		}

		FlushSln();
		return true;
	}

	private void FlushSln()
	{
		codeBuilder.AppendLine("Microsoft Visual Studio Solution File, Format Version 11.00");
		codeBuilder.AppendLine("# Visual Studio 2017");
		codeBuilder.AppendLine("VisualStudioVersion = 17.0.31314.256");
		codeBuilder.AppendLine("MinimumVisualStudioVersion = 10.0.40219.1");
		foreach (var (key, proj) in SubProjectsByName)
		{
			codeBuilder.AppendLine(
				$"Project(\"{{{proj.guid}}}\") = \"{proj.name}\", \"{proj.fullPath.RelativeTo(outputFolder)}\", \"{{{proj.guid}}}\"");
			codeBuilder.AppendLine("EndProject");
		}

		codeBuilder.AppendLine("Global");
		{
			codeBuilder.AddIndent();

			codeBuilder.AppendLine("GlobalSection(SolutionConfigurationPlatforms) = preSolution");
			{
				codeBuilder.AddIndent();

				var firstProj = SubProjects.First();
				foreach (var c in firstProj.Value.projectConfigs)
				{
					codeBuilder.AppendLine($"{c.ConfigurationName}|{c.PlatformName} = {c.ConfigurationName}|{c.PlatformName}");
				}
				
				codeBuilder.RemoveIndent();
			}
			codeBuilder.AppendLine("EndGlobalSection");
			
			codeBuilder.AppendLine("GlobalSection(ProjectConfigurationPlatforms) = postSolution");
			{
				codeBuilder.AddIndent();

				foreach (var (key, proj) in SubProjectsByName)
				{
					foreach (var c in proj.projectConfigs)
					{
						codeBuilder.AppendLine($"{{{proj.guid}}}.{c.ConfigurationName}|{c.PlatformName}.ActiveCfg = {c.ConfigurationName}|{c.PlatformName}");
						codeBuilder.AppendLine($"{{{proj.guid}}}.{c.ConfigurationName}|{c.PlatformName}.Build.0 = {c.ConfigurationName}|{c.PlatformName}");
					}
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
	
	public Dictionary<string, ISlnSubProject> SubProjectsByName { get; } =
		new Dictionary<string, ISlnSubProject>();
	public Dictionary<Guid, ISlnSubProject> SubProjects { get; } = new Dictionary<Guid, ISlnSubProject>();

	private NPath outputFolder;
	private SourceCodeBuilder codeBuilder = new SourceCodeBuilder();
	
}