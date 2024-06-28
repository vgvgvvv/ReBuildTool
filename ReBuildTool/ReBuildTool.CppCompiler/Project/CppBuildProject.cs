using System.Reflection;
using NiceIO;
using ReBuildTool.Service.CompileService;
using ReBuildTool.Service.Context;
using ReBuildTool.Service.IDEService.VisualStudio;

namespace ReBuildTool.ToolChain.Project;

public interface ICppSourceProvider
{
	NPath ProjectRoot { get; }
	Dictionary<string, TargetRule> TargetRules { get; }
	Dictionary<string, ModuleRule> ModuleRules { get; }
}

public class CppBuildProject : ICppSourceProvider, ICppProject
{
	public const string TargetDefineExtension = ".Target.cs";
	public const string ModuleDefineExtension = ".Module.cs";
	public const string ExtensionDefineExtension = ".Extension.cs";
	
	private CppBuildProject(NPath workDirectory)
	{
		ProjectRoot = workDirectory;
	}

	private void ParseRules()
	{
		var targetFiles = ProjectRoot.Files($"*{TargetDefineExtension}", true).ToList();
		var moduleFiles = ProjectRoot.Files($"*{ModuleDefineExtension}", true).ToList();
		var extraFiles = ProjectRoot.Files($"*{ExtensionDefineExtension}", true).ToList();
		
		foreach (var targetFile in targetFiles)
		{
			TargetRulePaths.Add(targetFile.FileNameWithoutExtension, targetFile);
		}
		
		foreach (var moduleFile in moduleFiles)
		{
			var fileName = moduleFile.FileName;
			ModuleRulePaths.Add(fileName.Substring(0, fileName.Length - ModuleDefineExtension.Length), moduleFile);
		}
		
		var compiler = ServiceContext.Instance.FindService<ICSharpCompilerService>().Value;
		
		BuildRuleCompileUnit = compiler.CreateAssemblyUnit();
		BuildRuleCompileUnit.SourceFiles.AddRange(targetFiles);
		BuildRuleCompileUnit.SourceFiles.AddRange(moduleFiles);
		BuildRuleCompileUnit.SourceFiles.AddRange(extraFiles);
		BuildRuleCompileUnit.ReferenceDlls.Add(Assembly.GetAssembly(typeof(CppBuildProject))!.Location.ToNPath());
		BuildRuleCompileUnit.FileName = "CompileRules";
	}

	public void Parse()
	{
		ParseRules();

		var compiler = ServiceContext.Instance.FindService<ICSharpCompilerService>().Value;
		var slnResult = ServiceContext.Instance.Create<ISlnGenerator>("CompileRules", ProjectRoot);

		if (!slnResult)
		{
			throw new Exception("cannot parse sln generator");
		}

		var slnGenerator = slnResult.Value;

		slnGenerator.GenerateOrGetNetCoreCSProj(BuildRuleCompileUnit, compiler.DefaultEnvironment,
			CppBuildRuleProjectOutput);

		slnGenerator.FlushProjectsAndSln();
	}

	public void Setup()
	{
		// generate dll && do init functions
		BuildRuleAssembly();
	}

	public void Build(string? targetName = null)
	{
		Build(targetName, null);
	}
	
	public void Build(string? targetName, IBuildConfigProvider? configProvider = null)
	{
		if (NeedReBuildRuleAssembly())
		{
			BuildRuleAssembly();
		}

		InitAllRule();
		
		CppBuilder builder = new CppBuilder(configProvider);

		// do build & build hooks	
		if (targetName == null)
		{
			BuildAll(builder);
			return;
		}

		if (!TargetRules.TryGetValue(targetName, out var targetRule))
		{
			BuildAll(builder);
			return;
		}
		
		Build(builder, targetRule);
		
	}

	private bool NeedReBuildRuleAssembly()
	{
		return !CppBuildRuleDllPath.Exists();
	}

	private void BuildRuleAssembly()
	{
		var compiler = ServiceContext.Instance.FindService<ICSharpCompilerService>();
		if (!compiler)
		{
			throw new Exception("cannot find compiler !!");
		}
		compiler.Value.Compile(CppBuildRuleBinaryOutput, new List<IAssemblyCompileUnit>()
		{
			BuildRuleCompileUnit
		}, compiler.Value.DefaultEnvironment);
	}

	private void InitAllRule()
	{
		var compileRuleAssembly = Assembly.LoadFile(CppBuildRuleDllPath);

		var targetRules = compileRuleAssembly.GetTypes()
			.Where(t => t.IsSubclassOf(typeof(TargetRule)) && !t.IsGenericType && !t.IsAbstract)
			.Select(t => Activator.CreateInstance(t) as TargetRule)
			.ToList();
		foreach (var rule in targetRules)
		{
			if (rule == null)
			{
				continue;
			}
			var ruleName = rule.GetType().Name;
			if (TargetRulePaths.TryGetValue(ruleName, out var targetRulePath))
			{
				rule.TargetDirectory = targetRulePath.Parent;
			}
			TargetRules.Add(ruleName, rule);
		}
		
		var moduleRules = compileRuleAssembly.GetTypes()
			.Where(t => t.IsSubclassOf(typeof(ModuleRule)) && !t.IsGenericType && !t.IsAbstract)
			.Select(t => Activator.CreateInstance(t) as ModuleRule)
			.ToList();
		
		foreach (var rule in moduleRules)
		{
			if (rule == null)
			{
				continue;
			}

			var ruleName = rule.GetType().Name;
			if (ModuleRulePaths.TryGetValue(ruleName, out var moduleRulePath))
			{
				rule.ModuleDirectory = moduleRulePath.Parent;
				rule.SourceDirectories.Add(moduleRulePath.Parent.Combine("Public"));
				rule.SourceDirectories.Add(moduleRulePath.Parent.Combine("Private"));
				rule.PublicIncludePaths.Add(moduleRulePath.Parent.Combine("Public"));
				rule.PrivateIncludePaths.Add(moduleRulePath.Parent.Combine("Private"));
			}
			else
			{
				throw new Exception($"cannot find module {ruleName}");
			}
			ModuleRules.Add(ruleName, rule);
		}
	}

	private void Build(CppBuilder builder, TargetRule targetRule)
	{
		builder.SetSource(this)
			.BuildTarget(targetRule);
	}
	
	private void BuildAll(CppBuilder builder)
	{
		foreach (var (key, targetRule) in TargetRules)
		{
			Build(builder, targetRule);
		}
	}

	public NPath ProjectRoot { get; }

	public NPath IntermediaFolder => ProjectRoot.Combine("Intermedia");
	
	public Dictionary<string, TargetRule> TargetRules { get; } = new();
	public Dictionary<string, ModuleRule> ModuleRules { get; } = new();

	private Dictionary<string, NPath> TargetRulePaths { get; } = new();
	private Dictionary<string, NPath> ModuleRulePaths { get; } = new();
	
	private IAssemblyCompileUnit BuildRuleCompileUnit { get; set; }
	private NPath CppBuildRuleProjectOutput => IntermediaFolder.Combine("CppBuildRule/Project");
	private NPath CppBuildRuleBinaryOutput => IntermediaFolder.Combine("CppBuildRule/Binary");
	private NPath CppBuildRuleDllPath => CppBuildRuleBinaryOutput.Combine($"{BuildRuleCompileUnit.FileName}.dll");

}