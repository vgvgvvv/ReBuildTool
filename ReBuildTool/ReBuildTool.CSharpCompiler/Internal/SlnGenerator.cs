using NiceIO;
using ReBuildTool.Common;

namespace ReBuildTool.CSharpCompiler;

internal class SlnGenerator
{
	private SlnGenerator(string name)
	{
		Name = name;
	}

	public static SlnGenerator Create(string name, NPath outputPath)
	{
		var result = new SlnGenerator(name);
		result.outputFolder = outputPath;
		result.outputFolder.EnsureDirectoryExists();
		return result;
	}
	
	public void RegisterCsProj(NetFrameworkCSProj proj)
	{
		if (!NetFrameworkCSProjs.TryGetValue(proj.guid, out var outProj))
		{
			NetFrameworkCSProjsByName.Add(proj.name, proj);
			NetFrameworkCSProjs.Add(proj.guid, proj);
		}
	}

	public bool GetCsProj(string name, out NetFrameworkCSProj? outProj)
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
				$"Project(\"{{{proj.guid}}}\") = \"{proj.name}\", \"{proj.name}.csproj\", \"{{{proj.guid}}}\"");
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
	
	public Dictionary<string, NetFrameworkCSProj> NetFrameworkCSProjsByName { get; } =
		new Dictionary<string, NetFrameworkCSProj>();
	public Dictionary<Guid, NetFrameworkCSProj> NetFrameworkCSProjs { get; } = new Dictionary<Guid, NetFrameworkCSProj>();

	private NPath outputFolder;
	private SourceCodeBuilder codeBuilder = new SourceCodeBuilder();
}