using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using NiceIO;
using ReBuildTool.Service.CompileService;
using ResetCore.Common;

namespace ReBuildTool.CSharpCompiler;

internal class SingleSimpleCompileUnitContext
{
	public SingleSimpleCompileUnitContext(IAssemblyCompileUnit unit, NPath outputPath)
	{
		TargetUnit = unit;
		OutputPath = outputPath;
		CSharpParseOptions = new(LanguageVersion.Latest);
		CSharpCompileOptions = new (OutputKind.DynamicallyLinkedLibrary);
	}
	public IAssemblyCompileUnit TargetUnit { get; }

	public string Name => TargetUnit.FileName;
	public NPath OutputPath { get; }
	public List<MetadataReference> ReferencesDlls { get; } = new ();
	public List<SingleSimpleCompileUnitContext> ReferenceCompileContext { get; } = new();
	public List<SyntaxTree> SyntaxTrees { get; } = new();
	public Compilation UnitCompilation { get; private set; }
	public CSharpParseOptions CSharpParseOptions { get; private set; }
	public CSharpCompilationOptions CSharpCompileOptions { get; } 

	public List<string> DefaultNamespaces { get; } = new List<string>();

	public DllCache CompiledDllCache { get; private set; } = new();

	public bool Parse(ICSharpCompileEnvironment env)
	{
		Log.Info($"Parse source files of {Name} begin");
		foreach (var sourceFile in TargetUnit.SourceFiles)
		{
			try
			{
				var text = sourceFile.ReadAllText();
				var stringText = SourceText.From(text, Encoding.UTF8);
				CSharpParseOptions
					.WithPreprocessorSymbols(env.Definitions);
				var tree = SyntaxFactory.ParseSyntaxTree(stringText, CSharpParseOptions, sourceFile.FileName);
				SyntaxTrees.Add(tree);
			}
			catch (Exception e)
			{
				Log.Error($"parse {sourceFile} failed !! {e}");
				Log.Exception(e);
				return false;
			}
		}
		Log.Info($"Parse source files of {Name} end");
		return true;
	}

	public bool TargetCompile(ICSharpCompileEnvironment env)
	{
		Log.Info($"Compile target {Name} begin");
		var compileType = 
			TargetUnit.CompileType == CompileOutputType.Library
				? OutputKind.DynamicallyLinkedLibrary
				: OutputKind.ConsoleApplication;
		var optLevel =
			env.Configuration == CSharpCompileConfiguration.Debug
				? OptimizationLevel.Debug
				: OptimizationLevel.Release;
		CSharpCompileOptions
			.WithAllowUnsafe(TargetUnit.Unsafe)
			.WithOutputKind(compileType)
			.WithOverflowChecks(true)
			.WithOptimizationLevel(optLevel);

		foreach (var referenceDll in TargetUnit.ReferenceDlls)
		{
			ReferencesDlls.Add(MetadataReference.CreateFromFile(referenceDll));
		}
		
		var frameworkPath = RuntimeEnvironment.GetRuntimeDirectory().ToNPath();
		foreach (var systemDll in frameworkPath.Files("*.dll", true))
		{
			if (!MonoUtil.IsDotNetAssembly(systemDll))
			{
				continue;
			}
			ReferencesDlls.Add(MetadataReference.CreateFromFile(systemDll));
		}
		
		foreach (var referenceCompile in ReferenceCompileContext)
		{
			if (!referenceCompile.CompiledDllCache.Location.Exists())
			{
				throw new Exception($"must compile {referenceCompile.Name} before {Name}");
			}
			ReferencesDlls.Add(MetadataReference.CreateFromFile(referenceCompile.CompiledDllCache.Location));
		}
		
		UnitCompilation = CSharpCompilation.Create(
			$"{TargetUnit.FileName}.dll",
			SyntaxTrees,
			ReferencesDlls,
			CSharpCompileOptions);

		try
		{
			OutputPath.EnsureDirectoryExists();
			var dllLocation = OutputPath.Combine($"{Name}.dll");
			var result = UnitCompilation.Emit(dllLocation);
			if (!result.Success)
			{
				foreach (var dia in result.Diagnostics)
				{
					switch (dia.Severity)
					{
						case DiagnosticSeverity.Warning:
							Log.Warning(dia.ToString());
							break;
						case DiagnosticSeverity.Error:
							Log.Error(dia.ToString());
							break;
						default:
							break;
					}
				}

				return false;
			}

			CompiledDllCache.Name = Name;
			CompiledDllCache.Location = dllLocation;
			return true;
		}
		catch (Exception e)
		{
			Log.Error($"error raised when compile {Name}");
			Log.Exception(e);
			return false;
		}
		finally
		{
			Log.Info($"Compile target {Name} end");
		}
	}

}

internal class SimpleCompileContext
{
	public SimpleCompileContext(CompileContext con)
	{
		Context = con;
	}

	public SingleSimpleCompileUnitContext CreateCompileUnit(IAssemblyCompileUnit unit)
	{
		var singleContext = new SingleSimpleCompileUnitContext(unit, Context.OutputPath.ToNPath());
		SingleCompileContexts.TryAdd(unit.FileName, singleContext);
		foreach (var refUnit in unit.References)
		{
			var refContext = CreateCompileUnit(refUnit);
			singleContext.ReferenceCompileContext.Add(refContext);
		}
		return singleContext;
	}
	
	public CompileContext Context { get; }
	public Dictionary<string, SingleSimpleCompileUnitContext> SingleCompileContexts { get; } = new();
	public List<SingleSimpleCompileUnitContext> CompileQueue = new();
}

public class SimpleCompiler : ICSharpCompilerBase
{
	internal override void Compile(CompileContext compileContext)
	{
		Context = new SimpleCompileContext(compileContext);

		foreach (var unit in compileContext.CompileUnits)
		{
			Context.CreateCompileUnit(unit);
		}

		if (!ParseAllSyntaxTree(compileContext.Env))
		{
			return;
		}

		if (!CompileAllUnit(compileContext.Env))
		{
			return;
		}
	}

	private bool ParseAllSyntaxTree(ICSharpCompileEnvironment env)
	{
		foreach (var (key, singleUnit) in Context.SingleCompileContexts)
		{
			singleUnit.CSharpParseOptions.WithLanguageVersion(LanguageVersion.Latest);
			if (!singleUnit.Parse(env))
			{
				return false;
			}
		}

		return true;
	}

	private bool CompileAllUnit(ICSharpCompileEnvironment env)
	{
		foreach (var (key, singleUnit) in Context.SingleCompileContexts)
		{
			EnqueueCompileUnit(singleUnit);
		}

		foreach (var targetToCompile in Context.CompileQueue)
		{
			if (!targetToCompile.TargetCompile(env))
			{
				Log.Error("compile failed !!");
				return false;
			}
		}

		return true;
	}

	private void EnqueueCompileUnit(SingleSimpleCompileUnitContext compileUnit)
	{
		if (Context.CompileQueue.Contains(compileUnit))
		{
			return;
		}
		foreach (var refedContext in compileUnit.ReferenceCompileContext)
		{
			EnqueueCompileUnit(refedContext);
		}
		Context.CompileQueue.Add(compileUnit);
	}

	private SimpleCompileContext Context { get; set; }
}