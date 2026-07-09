using NiceIO;
using ReBuildTool.Common;
using ReBuildTool.Service.Global;
using ReBuildTool.Service.CompileService;
using ReBuildTool.Service.IDEService.VisualStudio;
using ReBuildTool.ToolChain;
using ResetCore.Common;

namespace ReBuildTool.IDE.VisualStudio;

public partial class VCProject
{
	// VS doesn't auto-detect the installed MSVC toolset here - bump this if it doesn't match.
	private const string PlatformToolsetVersion = "v143";

	private void GenerateProject()
	{
		projectCodeBuilder.Builder.Clear();
		projectCodeBuilder.WriteHeader();
		using (projectCodeBuilder.CreateXmlScope(Tags.Project,
			       new Tuple<string, string>("DefaultTargets", "Build"),
			       new Tuple<string, string>("ToolsVersion", "17.0"),
			       new Tuple<string, string>("xmlns", "http://schemas.microsoft.com/developer/msbuild/2003")))
		{
			GenerateProjectConfigurationList();
			GenerateGlobalProperty();
			GenerateImportDefaultProps();
			// ConfigurationType must be declared before Microsoft.Cpp.props is imported, otherwise
			// VS evaluates it as a normal (non-Makefile) project and ignores all NMake* settings.
			GenerateConfigurationTypePropertyGroups();
			GenerateImportCppProps();
			GenerateNMakePropertyGroups();
			GenerateSourceFile();
			GenerateImportCppTargets();
		}
	}

	private void GenerateProjectConfigurationList()
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
	}

	private void GenerateConfigurationTypePropertyGroups()
	{
		foreach (var configuration in projectConfigs)
		{
			using (projectCodeBuilder.CreateXmlScope(Tags.PropertyGroup,
				       new Tuple<string, string>("Label", "Configuration"),
				       new Tuple<string, string>("Condition",
					       $"'$(Configuration)|$(Platform)'=='{configuration.ConfigurationName}|{configuration.PlatformName}'")))
			{
				projectCodeBuilder.WriteNode("ConfigurationType", "Makefile");
				projectCodeBuilder.WriteNode("PlatformToolset", PlatformToolsetVersion);
			}
		}
	}

	private void GenerateNMakePropertyGroups()
	{
		foreach (var configuration in projectConfigs)
		{
			GenerateNMakePropertyGroup(configuration);
		}
	}

	private void GenerateNMakePropertyGroup(IProjectConfiguration configuration)
	{
		using (projectCodeBuilder.CreateXmlScope("PropertyGroup",
			       new Tuple<string, string>("Condition",
				       $"'$(Configuration)|$(Platform)'=='{configuration.ConfigurationName}|{configuration.PlatformName}'")))
		{
			// Always go through $RBT_HOME/rbt.bat so this stays correct regardless of which
			// ReBuildTool build (dev checkout vs Booster-installed) generated this project.
			var rbtExe = GlobalPaths.ReBuildToolHome.Combine("rbt.bat");
			var commonArgs = $"{rbtExe.InQuotes()} --ProjectRoot {cppSource.ProjectRoot.InQuotes()} " +
			                  $"--BuildConfig {configuration.ConfigurationName} --TargetArch {configuration.PlatformName}";

			projectCodeBuilder.WriteNode("NMakeBuildCommandLine", $"{commonArgs} --Mode Build");
			projectCodeBuilder.WriteNode("NMakeReBuildCommandLine", $"{commonArgs} --Mode ReBuild");
			projectCodeBuilder.WriteNode("NMakeCleanCommandLine", $"{commonArgs} --Mode Clean");
			projectCodeBuilder.WriteNode("NMakeOutput", GetNMakeOutput(configuration));
			projectCodeBuilder.WriteNode("NMakePreprocessorDefinitions", string.Join(';', GetAllPreprocessorDefinitions()));
			projectCodeBuilder.WriteNode("NMakeIncludeSearchPath", string.Join(';', GetAllIncludeSearchPaths()));
		}
	}

	private string GetNMakeOutput(IProjectConfiguration configuration)
	{
		var exeModule = FindExecutableModule();
		if (exeModule == null)
		{
			return string.Empty;
		}

		var ext = generatorConfigProvider.ToolChain.ExecutableExtension;
		var platformName = IPlatformSupport.CurrentTargetPlatform.ToString();
		return cppSource.OutputRoot
			.Combine(platformName)
			.Combine(configuration.ConfigurationName)
			.Combine(configuration.PlatformName)
			.Combine(exeModule.TargetName + ext);
	}

	private IModuleInterface? FindExecutableModule()
	{
		return cppSource.ModuleRules.Values
			.FirstOrDefault(module => module.TargetBuildType == BuildType.Executable);
	}

	private void GenerateGlobalProperty()
	{
		using (projectCodeBuilder.CreateXmlScope(Tags.PropertyGroup, new Tuple<string, string>("Label", "Globals")))
		{
			projectCodeBuilder.WriteNode("ProjectGuid", guid.ToString());
		}
	}

	private void GenerateImportDefaultProps()
	{
		projectCodeBuilder.WriteNodeWithoutValue(Tags.Import, new Tuple<string, string>("Project", "$(VCTargetsPath)\\Microsoft.Cpp.Default.props"));
	}

	private void GenerateImportCppProps()
	{
		projectCodeBuilder.WriteNodeWithoutValue(Tags.Import, new Tuple<string, string>("Project", commonPropPath));
	}

	private void GenerateImportCppTargets()
	{
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
			var moduleDir = moduleInterface.ModuleDirectory.ToNPath();
			var currentPlatform = IPlatformSupport.CurrentTargetPlatform.ToString();
			foreach (var includeFile in moduleDir.Files(true).Where(IsHeader).Where(f => f.IsPlatformMatch(moduleDir, currentPlatform)))
			{
				projectCodeBuilder.WriteNodeWithoutValue("ClInclude",
					new Tuple<string, string>("Include", includeFile.RelativeTo(outputFolder)));
			}

			foreach (var includeFile in moduleDir.Files(true).Where(IsSource).Where(f => f.IsPlatformMatch(moduleDir, currentPlatform)))
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

	private IEnumerable<string> GetDefinesForModule(IModuleInterface module)
	{
		var depModules = new HashSet<IModuleInterface>();
		GetDependencies(module, depModules);
		foreach (var dep in depModules)
		{
			foreach (var define in dep.PublicDefines)
			{
				yield return define;
			}
		}

		foreach (var define in module.PrivateDefines)
		{
			yield return define;
		}

		foreach (var define in module.PublicDefines)
		{
			yield return define;
		}
	}

	// NMakePreprocessorDefinitions/NMakeIncludeSearchPath are project-level (not per-file) settings -
	// they're what VS's IntelliSense actually reads for a Makefile project, so aggregate across every
	// module in the project rather than per module like GetOptionsForModule/GetIncludeDirectoriesForModule.
	private IEnumerable<string> GetAllPreprocessorDefinitions()
	{
		var result = new List<string>();
		foreach (var module in cppSource.ModuleRules.Values)
		{
			result.AddRange(GetDefinesForModule(module));
		}
		return result.Distinct();
	}

	private IEnumerable<string> GetAllIncludeSearchPaths()
	{
		var result = new List<string>();
		foreach (var module in cppSource.ModuleRules.Values)
		{
			result.AddRange(GetIncludeDirectoriesForModule(module).Select(path => path.ToString()));
		}
		return result.Distinct();
	}

}