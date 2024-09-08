using NiceIO;
using ReBuildTool.CppCompiler.Standalone;
using ResetCore.Common;

namespace ReBuildTool.ToolChain;

public partial class CppBuilder
{
	internal partial class CompileProcess
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
			foreach (var sourceDirectory in Module.SourceDirectories)
			{
				var files = sourceDirectory.ToNPath().Files(true)
					.Where(f => ToolChain.CanBeCompiled(f))
					.ToList();
				foreach (var sourceFile in files)
				{
					var compileUnit = new CppCompilationUnit();
					compileUnit.SourceFile = sourceFile;
					compileUnit.CompileFlags = GetCompileFlagsForCompileUnit(compileUnit);
					compileUnit.Defines = GetDefinesForCompileUnit(compileUnit);
					compileUnit.IncludePaths = GetIncludePathsForCompileUnit(compileUnit);
					compileUnit.OutputFile = ObjectCachePath(sourceFile);
					compileUnit.CompileArgsBuilder = ToolChain.MakeCompileArgsBuilder();
					if (Module is ModuleRule moduleRule)
					{
						moduleRule.AdditionCompileArgs(compileUnit.CompileArgsBuilder);
					}
					CompileUnits.Add(compileUnit);
				}
			}

			return true;
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

			if (Module is ModuleRule moduleRule)
			{
				foreach (var define in moduleRule.DefinesFor(unit))
				{
					yield return define;
				}
			}
			
			foreach (var define in Options.CustomDefines)
			{
				yield return define;
			}
		}
		
		private IEnumerable<string> GetCompileFlagsForCompileUnit(CppCompilationUnit unit)
		{
			foreach (var compileFlag in GetCompileFlagsForModule(Module))
			{
				yield return compileFlag;
			}

			if (Module is ModuleRule moduleRule)
			{
				foreach (var compileFlag in moduleRule.CompileFlagsFor(unit))
				{
					yield return compileFlag;
				}
			}
			
			foreach (var compileFlag in Options.CustomCompileFlags)
			{
				yield return compileFlag;
			}
		}
		
		private IEnumerable<NPath> GetIncludePathsForCompileUnit(CppCompilationUnit unit)
		{
			foreach (var includePath in GetIncludePathsForModule(Module))
			{
				yield return includePath.ToNPath();
			}

			if (Module is ModuleRule moduleRule)
			{
				foreach (var includePath in moduleRule.IncludePathsFor(unit))
				{
					yield return includePath.ToNPath();
				}
			}
			
			
			foreach (var includePath in Options.CustomIncludeDirectories)
			{
				yield return includePath;
			}
		}

		#endregion
		
		private List<CppCompilationUnit> CompileUnits { get; } = new();

		private List<CppCompileInvocation> CompileInvocation { get; } = new();
	}
}