using NiceIO;
using ReBuildTool.Service.CompileService;
using ReBuildTool.ToolChain.Project;
using ResetCore.Common;

namespace ReBuildTool.ToolChain;

public partial class CppBuilder
{
	public partial class CompileProcess
	{
		public static CompileProcess Create(IModuleInterface module, CppBuilder owner)
		{
			return new CompileProcess(module, owner);
		}
		
		private CompileProcess(IModuleInterface module, CppBuilder owner)
		{
			Module = module;
			Owner = owner;
		}
		

		#region ModuleInfos

		internal IEnumerable<string> GetDefinesForModule(IModuleInterface module)
		{
			var depModules = ModuleDependencies(module);
			foreach (var depModule in depModules)
			{
				foreach (var define in depModule.PublicDefines)
				{
					yield return define;
				}
			}

			foreach (var publicDef in module.PublicDefines)
			{
				yield return publicDef;
			}
			
			foreach (var publicDef in module.PrivateDefines)
			{
				yield return publicDef;
			}
			
			var platformName = IPlatformSupport.SupprtedPlatforms
				.First(kvp => kvp.Value.GetType() == Owner.CurrentPlatformSupport.GetType()).Key.ToString().ToUpper();
			yield return $"PLATFORM_{platformName}";

			var archName = Owner.CurrentBuildOption.Architecture.Name.ToUpper();
			yield return $"ARCH_{archName}";
			
			var configurationName = Owner.CurrentBuildOption.Configuration.ToString().ToUpper();
			yield return $"CONFIGURATION_{configurationName}";

			var targetNameUpper = module.TargetName.ToUpper();
			if (module.TargetBuildType == BuildType.StaticLibrary)
			{
				yield return $"{targetNameUpper}_BUILT_AS_STATIC";
				yield return $"{targetNameUpper}_NO_EXPORTS";
			}
			else if (module.TargetBuildType == BuildType.DynamicLibrary)
			{
				yield return $"{targetNameUpper}_BUILT_AS_DYNAMIC";
				yield return $"{targetNameUpper}_EXPORTS";
			}
			else if (module.TargetBuildType == BuildType.Executable)
			{
				yield return $"{targetNameUpper}_BUILT_AS_EXECUTABLE";
				yield return $"{targetNameUpper}_EXPORTS";
			}
			
			foreach (var define in Options.CustomDefines)
			{
				yield return define;
			}
		}
		
		internal IEnumerable<string> GetCompileFlagsForModule(IModuleInterface module)
		{
			var depModules = ModuleDependencies(module);
			foreach (var depModule in depModules)
			{
				foreach (var define in depModule.PublicCompileFlags)
				{
					yield return define;
				}
			}

			foreach (var publicDef in module.PublicCompileFlags)
			{
				yield return publicDef;
			}

			foreach (var publicDef in module.PrivateCompileFlags)
			{
				yield return publicDef;
			}
			
			foreach (var compileFlag in Options.CustomCompileFlags)
			{
				yield return compileFlag;
			}
		}

		internal IEnumerable<string> GetLinkFlagsForModule(IModuleInterface module)
		{
			var depModules = ModuleDependencies(module);
			foreach (var depModule in depModules)
			{
				foreach (var define in depModule.PublicLinkFlags)
				{
					yield return define;
				}
			}

			foreach (var publicDef in module.PublicLinkFlags)
			{
				yield return publicDef;
			}

			foreach (var publicDef in module.PrivateLinkFlags)
			{
				yield return publicDef;
			}
		}
		
		internal IEnumerable<string> GetArchiveFlagsForModule(IModuleInterface module)
		{
			var depModules = ModuleDependencies(module);
			foreach (var depModule in depModules)
			{
				foreach (var define in depModule.PublicArchiveFlags)
				{
					yield return define;
				}
			}

			foreach (var publicDef in module.PublicArchiveFlags)
			{
				yield return publicDef;
			}

			foreach (var publicDef in module.PrivateArchiveFlags)
			{
				yield return publicDef;
			}
		}
		
		internal IEnumerable<string> GetIncludePathsForModule(IModuleInterface module)
		{
			var depModules = ModuleDependencies(module);
			foreach (var depModule in depModules)
			{
				foreach (var define in depModule.PublicIncludePaths)
				{
					yield return define;
				}
			}

			foreach (var publicDef in module.PublicIncludePaths)
			{
				yield return publicDef;
			}

			foreach (var publicDef in module.PrivateIncludePaths)
			{
				yield return publicDef;
			}
			
			foreach (var includePath in Options.CustomIncludeDirectories)
			{
				yield return includePath;
			}
		}

		internal IEnumerable<string> GetStaticLibrariesForModule(IModuleInterface module)
		{
			var depModules = ModuleDependencies(module);
			foreach (var depModule in depModules)
			{
				foreach (var define in depModule.PublicStaticLibraries)
				{
					yield return define;
				}
			}

			foreach (var publicDef in module.PublicStaticLibraries)
			{
				yield return publicDef;
			}

			foreach (var publicDef in module.PrivateStaticLibraries)
			{
				yield return publicDef;
			}
		}
		
		internal IEnumerable<string> GetDynamicLibrariesForModule(IModuleInterface module)
		{
			var depModules = ModuleDependencies(module);
			foreach (var depModule in depModules)
			{
				foreach (var define in depModule.PublicDynamicLibraries)
				{
					yield return define;
				}
			}

			foreach (var publicDef in module.PublicDynamicLibraries)
			{
				yield return publicDef;
			}

			foreach (var publicDef in module.PrivateDynamicLibraries)
			{
				yield return publicDef;
			}
		}
		
		internal IEnumerable<string> GetLibraryDirectoriesForModule(IModuleInterface module)
		{
			var depModules = ModuleDependencies(module);
			foreach (var depModule in depModules)
			{
				foreach (var define in depModule.PublicLibraryDirectories)
				{
					yield return define;
				}
			}

			foreach (var publicDef in module.PublicLibraryDirectories)
			{
				yield return publicDef;
			}

			foreach (var publicDef in module.PrivateLibraryDirectories)
			{
				yield return publicDef;
			}
		}

		internal IEnumerable<IModuleInterface> ModuleDependencies(IModuleInterface module, HashSet<string>? checkedModules = null)
		{
			if(checkedModules == null)
			{
				checkedModules = new HashSet<string>();
			}
			foreach (var depModuleName in module.Dependencies)
			{
				if (checkedModules.Contains(depModuleName))
				{
					continue;
				}
				var depModule = GetModuleRuleByName(depModuleName);
				if (depModule == null)
				{
					continue;
				}
				yield return depModule;
				var dependencies = ModuleDependencies(depModule, checkedModules);
				foreach (var moduleRule in dependencies)
				{
					yield return moduleRule;
				}
			}
		}

		#endregion
		
		private IModuleInterface? GetModuleRuleByName(string name)
		{
			Source.ModuleRules.TryGetValue(name, out var moduleRule);
			return moduleRule;
		}

	
		
		public IModuleInterface Module { get; }
		public CppBuilder Owner { get; }

		private IToolChain ToolChain => Owner.CurrentToolChain;

		private BuildOptions Options => Owner.CurrentBuildOption;

		private ICppSourceProviderInterface Source => Owner.CurrentSource;
	}

	
}