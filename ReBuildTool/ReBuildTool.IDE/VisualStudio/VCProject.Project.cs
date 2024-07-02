using NiceIO;
using ReBuildTool.Common;
using ReBuildTool.Service.CompileService;
using ReBuildTool.Service.IDEService.VisualStudio;
using ReBuildTool.ToolChain;
using ResetCore.Common;

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
			using (projectCodeBuilder.CreateXmlScope("ClCompile",
				       new Tuple<string, string>("Include", includeFile.RelativeTo(outputFolder))))
			{
				List<string> additionalOptions = new();
				additionalOptions.AddRange(GetOptionsForModule(moduleInterface));
				projectCodeBuilder.WriteNode("AdditionalOptions", string.Join(';', additionalOptions));

				List<NPath> additionalIncludeDirectories = new();
				additionalIncludeDirectories.AddRange(GetIncludeDirectoriesForModule(moduleInterface));
				projectCodeBuilder.WriteNode("AdditionalIncludeDirectories",
					string.Join(';', additionalIncludeDirectories));
				
				List<NPath> additionalForcedIncludeFiles = new();
				// TODO: pch
				projectCodeBuilder.WriteNode("ForcedIncludeFiles",
					string.Join(';', additionalForcedIncludeFiles));
			}
		}
	}

	private void GetDependencies(IModuleInterface module, HashSet<IModuleInterface> outModules, bool top = true)
	{
		if (outModules.Contains(module))
		{
			return;
		}
		foreach (var moduleName in module.Dependencies)
		{
			if (cppSource.ModuleRules.TryGetValue(moduleName, out var moduleInterface))
			{
				outModules.Add(moduleInterface);
				GetDependencies(moduleInterface, outModules, false);
			}
			else
			{
				Log.Error($"cannot find module {moduleName} used by {module.TargetName}");
			}
		}

		if (top)
		{
			outModules.Remove(module);
		}
	}

	private IEnumerable<string> GetOptionsForModule(IModuleInterface module)
	{
		var depModules = new HashSet<IModuleInterface>();
		GetDependencies(module, depModules);
		foreach (var dep in depModules)
		{
			foreach (var flag in dep.PublicCompileFlags)
			{
				yield return flag;
			}
		}
		
		// cannot approve module flag for each unit by virtual method
		
		foreach (var flag in module.PrivateCompileFlags)
		{
			yield return flag;
		}
		
		foreach (var flag in module.PublicCompileFlags)
		{
			yield return flag;
		}
		
		foreach (var path in generatorConfigProvider.ToolChain.ToolChainIncludePaths())
		{
			yield return path;
		}
	}
	
	private IEnumerable<NPath> GetIncludeDirectoriesForModule(IModuleInterface module)
	{
		var depModules = new HashSet<IModuleInterface>();
		GetDependencies(module, depModules);
		foreach (var dep in depModules)
		{
			foreach (var path in dep.PublicIncludePaths)
			{
				yield return path.ToNPath();
			}
		}
		
		// cannot approve module flag for each unit by virtual method
		
		foreach (var path in module.PrivateIncludePaths)
		{
			yield return path.ToNPath();
		}
		
		foreach (var path in module.PublicIncludePaths)
		{
			yield return path.ToNPath();
		}
		
		foreach (var path in generatorConfigProvider.ToolChain.ToolChainIncludePaths())
		{
			yield return path;
		}
	}

}