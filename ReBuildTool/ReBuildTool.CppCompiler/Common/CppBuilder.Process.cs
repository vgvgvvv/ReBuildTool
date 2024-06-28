using NiceIO;
using ReBuildTool.ToolChain.Project;
using ResetCore.Common;

namespace ReBuildTool.ToolChain;

public partial class CppBuilder
{
	internal partial class CompileProcess
	{
		public static CompileProcess Create(ModuleRule module, CppBuilder owner)
		{
			return new CompileProcess(module, owner);
		}
		
		private CompileProcess(ModuleRule module, CppBuilder owner)
		{
			Module = module;
			Owner = owner;
		}
		

		#region ModuleInfos

		private IEnumerable<string> GetDefinesForModule(ModuleRule module)
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
		}
		
		private IEnumerable<string> GetCompileFlagsForModule(ModuleRule module)
		{
			var depModules = ModuleDependencies(module);
			foreach (var depModule in depModules)
			{
				foreach (var define in depModule.PublicCompilerFlags)
				{
					yield return define;
				}
			}

			foreach (var publicDef in module.PublicCompilerFlags)
			{
				yield return publicDef;
			}

			foreach (var publicDef in module.PrivateCompileFlags)
			{
				yield return publicDef;
			}
		}

		private IEnumerable<string> GetLinkFlagsForModule(ModuleRule module)
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
		
		private IEnumerable<string> GetArchiveFlagsForModule(ModuleRule module)
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
		
		private IEnumerable<string> GetIncludePathsForModule(ModuleRule module)
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
		}

		private IEnumerable<string> GetStaticLibrariesForModule(ModuleRule module)
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
		
		private IEnumerable<string> GetDynamicLibrariesForModule(ModuleRule module)
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
		
		private IEnumerable<string> GetLibraryDirectoriesForModule(ModuleRule module)
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

		private IEnumerable<ModuleRule> ModuleDependencies(ModuleRule module, HashSet<string>? checkedModules = null)
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
		
		private ModuleRule? GetModuleRuleByName(string name)
		{
			Source.ModuleRules.TryGetValue(name, out var moduleRule);
			return moduleRule;
		}

	
		
		public ModuleRule Module { get; }
		public CppBuilder Owner { get; }

		private IToolChain ToolChain => Owner.CurrentToolChain;

		private BuildOptions Options => Owner.CurrentBuildOption;

		private ICppSourceProvider Source => Owner.CurrentSource;
	}

	
}