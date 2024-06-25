using NiceIO;

namespace ReBuildTool.ToolChain;

public partial class CppBuilder
{
	internal class CompileProcess
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
		
		public void Compile()
		{
			CollectCompileUnit();
			CollectCompileInvocations();
			RunCompileInvocations();
		}

		public void Link()
		{
			
		}
		
		private void CollectCompileUnit()
		{
			CompileUnits.Clear();
			foreach (var sourceDirectory in Module.SourceDirectories)
			{
				var files = sourceDirectory.ToNPath().Files(true)
					.Where(f => ToolChain.CanBeCompiled(f))
					.ToList();
				foreach (var sourceFile in files)
				{
					var compileUnit = new CppCompilationUnit();
					compileUnit.SourceFile = sourceFile;
					compileUnit.CompileFlags = GetCompileFlagsForUnit(compileUnit);
					compileUnit.Defines = GetDefinesForUnit(compileUnit);
					compileUnit.IncludePaths = GetIncludePathsForUnit(compileUnit);
				}
			}
		}

		private void CollectCompileInvocations()
		{
			
		}

		private void RunCompileInvocations()
		{
			
		}
		
		private IEnumerable<string> GetDefinesForUnit(CppCompilationUnit unit)
		{
			foreach (var toolChainDefine in ToolChain.ToolChainDefines())
			{
				yield return toolChainDefine;
			}

			foreach (var define in GetDefinesForModule(Module))
			{
				yield return define;
			}
		}
		
		private IEnumerable<string> GetCompileFlagsForUnit(CppCompilationUnit unit)
		{
			foreach (var compileFlag in ToolChain.CompilerFlagsFor(unit))
			{
				yield return compileFlag;
			}
			
			foreach (var compileFlag in GetCompileFlagsForModule(Module))
			{
				yield return compileFlag;
			}
		}
		
		private IEnumerable<NPath> GetIncludePathsForUnit(CppCompilationUnit unit)
		{
			foreach (var toolChainDefine in ToolChain.ToolChainIncludePaths())
			{
				yield return toolChainDefine;
			}
			
			foreach (var includePath in GetIncludePathsForModule(Module))
			{
				yield return includePath.ToNPath();
			}
		}

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

		private IEnumerable<string> GetLibrariesForModule(ModuleRule module)
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

			foreach (var publicDef in module.PrivateLibraries)
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
		
		private ModuleRule? GetModuleRuleByName(string name)
		{
			Source.ModuleRules.TryGetValue(name, out var moduleRule);
			return moduleRule;
		}

		private List<CppCompilationUnit> CompileUnits { get; } = new();

		private List<CppCompileInvocation> CompileInvocation { get; } = new();
		
		public ModuleRule Module { get; }
		public CppBuilder Owner { get; }

		private IToolChain ToolChain => Owner.CurrentToolChain;

		private BuildOptions Options => Owner.CurrentBuildOption;

		private ICppSourceProvider Source => Owner.CurrentSource;
	}

	internal class CppCompileInvocation
	{
		
	}
}