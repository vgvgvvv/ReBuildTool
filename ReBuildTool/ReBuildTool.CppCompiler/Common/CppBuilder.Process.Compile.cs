using NiceIO;
using ReBuildTool.CppCompiler;
using ReBuildTool.Common;
using ReBuildTool.Service.CompileService;
using ResetCore.Common;

namespace ReBuildTool.ToolChain;

public partial class CppBuilder
{
	public partial class CompileProcess
	{
		
		public bool Compile()
		{
			if (!CollectCompileUnit())
			{
				return false;
			}

			if (!CollectCompileInvocations())
			{
				return false;
			}

			if (!RunCompileInvocations())
			{
				return false;
			}

			return true;
		}

		private NPath ObjectCachePath(NPath sourceFile)
		{
			var relatePath = sourceFile.RelativeTo(Source.ProjectRoot);
			var result = Source.ProjectRoot
				.Combine("Intermedia", IPlatformSupport.CurrentTargetPlatform.ToString())
				.Combine(Options.Configuration.ToString())
				.Combine(Options.Architecture.Name)
				.Combine("ObjectCache", relatePath)
				.ChangeExtension(ToolChain.ObjectExtension);
			result.EnsureParentDirectoryExists();
			return result;
		}

		
			private bool CollectCompileUnit()
			{
				CompileUnits.Clear();

				// Pre-resolve ExcludeDirectories / ExcludeFiles to absolute NPaths
				// (relative entries are resolved against the module dir) so the
				// exclusion predicate can do fast membership tests.
				var rule = Module as CppModuleRule;
				var excludeDirs = (rule?.ExcludeDirectories ?? Enumerable.Empty<string>())
					.Select(p => rule!.ResolveSourcePath(p).ToNPath())
					.Where(d => d.Exists())
					.ToList();
				var excludeFiles = (rule?.ExcludeFiles ?? Enumerable.Empty<string>())
					.Select(p => rule!.ResolveSourcePath(p).ToNPath())
					.ToList();

				var currentPlatform = IPlatformSupport.CurrentTargetPlatform.ToString();

				foreach (var sourceDirectory in Module.SourceDirectories)
				{
					var sourceRoot = sourceDirectory.ToNPath();
					var files = sourceRoot.Files(true)
						.Where(f => ToolChain.CanBeCompiled(f))
						.Where(f => f.IsPlatformMatch(sourceRoot, currentPlatform))
						.Where(f => !IsExcluded(f, excludeDirs, excludeFiles))
						.ToList();
					foreach (var sourceFile in files)
					{
						AddCompileUnit(sourceFile);
					}
				}

				// Explicit per-file sources (on top of the directory globs).
				if (rule != null)
				{
					foreach (var rel in rule.SourceFiles)
					{
						var sourceFile = rule.ResolveSourcePath(rel).ToNPath();
						if (!sourceFile.Exists())
						{
							continue;
						}
						if (!ToolChain.CanBeCompiled(sourceFile))
						{
							continue;
						}
						if (IsExcluded(sourceFile, excludeDirs, excludeFiles))
							{
								continue;
							}
								if (!sourceFile.IsPlatformMatch(rule.ModuleDirectory.ToNPath(), currentPlatform))
							{
								continue;
							}
						// Skip if the directory glob already collected this file.
						if (CompileUnits.Any(u => u.SourceFile == sourceFile))
						{
							continue;
						}
						AddCompileUnit(sourceFile);
					}
				}

				return true;
			}

			private void AddCompileUnit(NPath sourceFile)
			{
				var compileUnit = new CppCompilationUnit(Module);
				compileUnit.SourceFile = sourceFile;
				compileUnit.CompileFlags = GetCompileFlagsForCompileUnit(compileUnit);
				compileUnit.Defines = GetDefinesForCompileUnit(compileUnit);
				compileUnit.IncludePaths = GetIncludePathsForCompileUnit(compileUnit);
				compileUnit.OutputFile = ObjectCachePath(sourceFile);
				compileUnit.CompileArgsBuilder = ToolChain.MakeCompileArgsBuilder();
				if (Module is CppModuleRule moduleRule)
				{
					moduleRule.AdditionCompileArgs(compileUnit.CompileArgsBuilder);
				}
				CompileUnits.Add(compileUnit);
			}

			/// <summary>
			/// True if <paramref name="file"/> is under one of the excluded
			/// directories or matches one of the excluded files. Compares via
			/// normalized string prefixes to cover subdirectory membership
			/// regardless of NPath's own equality semantics.
			/// </summary>
			private static bool IsExcluded(NPath file, List<NPath> excludeDirs, List<NPath> excludeFiles)
			{
				var filePath = file.ToString();
				foreach (var dir in excludeDirs)
				{
					var dirPath = dir.ToString();
					if (filePath.StartsWith(dirPath))
					{
						return true;
					}
				}
				foreach (var excluded in excludeFiles)
				{
					if (file == excluded || filePath == excluded.ToString())
					{
						return true;
					}
				}
				return false;
			}

		private bool CollectCompileInvocations()
		{
			foreach (var compileUnit in CompileUnits)
			{
				var invocation = ToolChain.MakeCompileInvocation(compileUnit);
				CompileInvocation.Add(invocation);
			}
			return true;
		}

		private bool RunCompileInvocations()
		{
			// TODO: use parallel
			int index = 1;
			int maxCount = CompileInvocation.Count;
			bool succ = true;
			foreach (var invocation in CompileInvocation)
			{
				if (CppCompilerArgs.Get().RunDry)
				{
					Log.Info($"Compile [{index}/{maxCount}]{invocation}");
					index++;
				}
				else
				{
					Log.Info($"Compile:[{index}/{maxCount}]");
					if (CppCompilerArgs.Get().DebugToolchainCmd)
					{
						Log.Info($"Cmd: {invocation}");
					}
					if (!invocation.Run())
					{
						Log.Error($"Compile failed: {invocation.ProgramName} {string.Join(' ', invocation.Arguments)}");
						succ = false;
					}
					index++;
				}
			}
			
			return succ;
		}
		
		#region CompileUnitInfo

		private IEnumerable<string> GetDefinesForCompileUnit(CppCompilationUnit unit)
		{
			foreach (var define in GetDefinesForModule(Module))
			{
				yield return define;
			}

			if (Module is CppModuleRule moduleRule)
			{
				foreach (var define in moduleRule.DefinesFor(unit))
				{
					yield return define;
				}
			}
		}
		
		private IEnumerable<string> GetCompileFlagsForCompileUnit(CppCompilationUnit unit)
		{
			foreach (var compileFlag in GetCompileFlagsForModule(Module))
			{
				yield return compileFlag;
			}

			if (Module is CppModuleRule moduleRule)
			{
				foreach (var compileFlag in moduleRule.CompileFlagsFor(unit))
				{
					yield return compileFlag;
				}
			}
		}
		
		private IEnumerable<NPath> GetIncludePathsForCompileUnit(CppCompilationUnit unit)
		{
			foreach (var includePath in GetIncludePathsForModule(Module))
			{
				yield return includePath.ToNPath();
			}

			if (Module is CppModuleRule moduleRule)
			{
				foreach (var includePath in moduleRule.IncludePathsFor(unit))
				{
					yield return includePath.ToNPath();
				}
			}
			
		}

		#endregion
		
		private List<CppCompilationUnit> CompileUnits { get; } = new();

		private List<CppCompileInvocation> CompileInvocation { get; } = new();
	}
}