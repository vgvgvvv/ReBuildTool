using System.Reflection;
using NiceIO;
using ReBuildTool.CSharpCompiler;
using ResetCore.Common;

namespace ReBuildTool.ToolChain;

public interface ICppSourceProvider
{
	Dictionary<string, TargetRule> TargetRules { get; }
	Dictionary<string, ModuleRule> ModuleRules { get; }
}

public class CppBuildProject : ICppSourceProvider
{
	public static CppBuildProject Create(string workDirectory)
	{
		return new CppBuildProject(workDirectory);
	}

	private CppBuildProject(string workDirectory)
	{
		ProjectRoot = workDirectory.ToNPath();
	}

	private void ParseRules()
	{
		var targetFiles = ProjectRoot.Files("*.Target.cs", true).ToList();
		var moduleFiles = ProjectRoot.Files("*.Module.cs", true).ToList();
		var extraFiles = ProjectRoot.Files("*.Extension.cs", true).ToList();
		
		foreach (var targetFile in targetFiles)
		{
			TargetRulePaths.Add(targetFile.FileNameWithoutExtension, targetFile);
		}
		
		foreach (var moduleFile in moduleFiles)
		{
			ModuleRulePaths.Add(moduleFile.FileNameWithoutExtension, moduleFile);
		}
		
		BuildRuleCompileUnit = new SimpleAssemblyCompileUnit();
		BuildRuleCompileUnit.SourceFiles.AddRange(targetFiles);
		BuildRuleCompileUnit.SourceFiles.AddRange(moduleFiles);
		BuildRuleCompileUnit.SourceFiles.AddRange(extraFiles);
		BuildRuleCompileUnit.ReferenceDlls.Add(Assembly.GetAssembly(typeof(CppBuildProject))!.Location.ToNPath());
		BuildRuleCompileUnit.FileName = "CompileRules";
	}

	public CppBuildProject Parse()
	{
		ParseRules();
		
		SlnGenerator slnGenerator = SlnGenerator.Create("CompileRules", ProjectRoot);
		NetCoreCSProj.GenerateOrGetCSProj(slnGenerator, BuildRuleCompileUnit, CompileEnvironment.Default,
			CppBuildRuleProjectOutput);

		slnGenerator.FlushProjectsAndSln();
		
		return this;
	}

	public void Setup()
	{
		// generate dll && do init functions
		BuildRuleAssembly();
	}

	public void Build(string? targetName = null, IBuildConfigProvider? configProvider = null)
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
		ICSharpCompiler.Default.Compile(CppBuildRuleBinaryOutput, new List<IAssemblyCompileUnit>()
		{
			BuildRuleCompileUnit
		});
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
			ModuleRules.Add(ruleName, rule);
		}
	}

	private void Build(CppBuilder builder, TargetRule targetRule)
	{
		builder.SetSource(this);
		builder.BuildTarget(targetRule);
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