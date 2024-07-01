using NiceIO;
using ReBuildTool.Common;
using ReBuildTool.Service.CompileService;
using ReBuildTool.Service.IDEService.VisualStudio;
using ReBuildTool.ToolChain;

namespace ReBuildTool.IDE.VisualStudio;

public partial class VCProject
{

	private void GenerateProject()
	{
		projectCodeBuilder.Builder.Clear();
		projectCodeBuilder.WriteHeader();
		using (projectCodeBuilder.CreateXmlScope(Tags.Project,
			       new Tuple<string, string>("DefaultTargets", "Build"),
			       new Tuple<string, string>("ToolsVersion", "17.0"),
			       new Tuple<string, string>("xmlns", "http://schemas.microsoft.com/developer/msbuild/2003")))
		{
			GenerateGlobalProperty();
			GenerateImports();
			GenerateProjectConfigurations();
			GenerateSourceFile();
		}
	}

	private void GenerateProjectConfigurations()
	{
		using(projectCodeBuilder.CreateXmlScope(Tags.ItemGroup, 
			      new Tuple<string, string>("Label", "ProjectConfigurations")))
		{
			foreach (var configuration in projectConfigs)
			{
				using (projectCodeBuilder.CreateXmlScope(Tags.ProjectConfiguration,
					       new Tuple<string, string>("Include",
						       $"{configuration.ConfigurationName}|{configuration.PlatformName}")))
				{
					projectCodeBuilder.WriteNode("Configuration", configuration.ConfigurationName);
					projectCodeBuilder.WriteNode("Platform", configuration.PlatformName);
				}
			}
		}
				
		foreach (var configuration in projectConfigs)
		{
			GenerateProjectConfiguration(configuration);
		}
	}

	private void GenerateProjectConfiguration(IProjectConfiguration configuration)
	{
		using (projectCodeBuilder.CreateXmlScope("PropertyGroup",
			       new Tuple<string, string>("Condition",
				       $"'$(Configuration)|$(Platform)'=='{configuration.ConfigurationName}|{configuration.PlatformName}'")))
		{
			// TODO: provide build command
			projectCodeBuilder.WriteNode("NMakeBuildCommandLine", "");
			projectCodeBuilder.WriteNode("NMakeReBuildCommandLine", "");
			projectCodeBuilder.WriteNode("NMakeCleanCommandLine", "");
			projectCodeBuilder.WriteNode("NMakeOutput", "");
			projectCodeBuilder.WriteNode("AdditionalOptions", "");
		}
	}
	

	private void GenerateGlobalProperty()
	{
		using (projectCodeBuilder.CreateXmlScope(Tags.PropertyGroup, new Tuple<string, string>("Label", "Globals")))
		{
			projectCodeBuilder.WriteNode("ProjectGuid", guid.ToString());
		}
	}

	private void GenerateImports()
	{
		projectCodeBuilder.WriteNodeWithoutValue(Tags.Import, new Tuple<string, string>("Project", commonPropPath));
		projectCodeBuilder.WriteNodeWithoutValue(Tags.Import, new Tuple<string, string>("Project", "$(VCTargetsPath)\\Microsoft.Cpp.targets"));
	}

	private void GenerateSourceFile()
	{
		
		using (projectCodeBuilder.CreateXmlScope(Tags.ItemGroup))
		{
			foreach (var file in cppSource.SourceFolder
				         .Files(true)
				         .Where(IsOther))
			{
				projectCodeBuilder.WriteNodeWithoutValue("None", 
					new Tuple<string, string>("Include", file.RelativeTo(outputFolder)));
			}
			
			foreach (var (key, target) in cppSource.TargetRules)
			{
				GenerateSourceFileFor(target);
			}

			foreach (var (key, module) in cppSource.ModuleRules)
			{
				GenerateSourceFileFor(module);
			}
		}
	}
	
	private void GenerateSourceFileFor(ITargetInterface targetInterface)
	{
		
	}

	private void GenerateSourceFileFor(IModuleInterface moduleInterface)
	{
		foreach (var includeFile in moduleInterface.ModuleDirectory.ToNPath()
			         .Files(true).Where(IsHeader))
		{
			projectCodeBuilder.WriteNodeWithoutValue("ClInclude", 
				new Tuple<string, string>("Include", includeFile.RelativeTo(outputFolder)));
		}
		
		foreach (var includeFile in moduleInterface.ModuleDirectory.ToNPath()
			         .Files(true).Where(IsSource))
		{
			projectCodeBuilder.WriteNodeWithoutValue("ClCompile", 
				new Tuple<string, string>("Include", includeFile.RelativeTo(outputFolder)));
		}
	}

}