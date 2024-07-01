using System.Reflection;
using NiceIO;
using ReBuildTool.Service.CompileService;
using ReBuildTool.Service.Context;
using ReBuildTool.Service.IDEService.VisualStudio;

namespace ReBuildTool.ToolChain.Project;

public interface ICppSourceProvider : ICppSourceProviderInterface
{
	string Name { get; }
	NPath ProjectRoot { get; }
	NPath SourceFolder { get; }
	NPath IntermediaFolder { get; }
	Dictionary<string, ITargetInterface> TargetRules { get; }
	Dictionary<string, IModuleInterface> ModuleRules { get; }
}

public class CppBuildProject : ICppSourceProvider, ICppProject
{

	
	private CppBuildProject(NPath workDirectory)
	{
		Name = workDirectory.FileName;
		ProjectRoot = workDirectory;
	}

	private void ParseRules()
	{
		var targetFiles = SourceFolder.Files($"*{ICppProject.TargetDefineExtension}", true).ToList();
		var moduleFiles = SourceFolder.Files($"*{ICppProject.ModuleDefineExtension}", true).ToList();
		var extraFiles = SourceFolder.Files($"*{ICppProject.ExtensionDefineExtension}", true).ToList();
		
		foreach (var targetFile in targetFiles)
		{
			TargetRulePaths.Add(targetFile.FileNameWithoutExtension, targetFile);
		}
		
		foreach (var moduleFile in moduleFiles)
		{
			var fileName = moduleFile.FileName;
			ModuleRulePaths.Add(fileName.Substring(0, fileName.Length - ICppProject.ModuleDefineExtension.Length), moduleFile);
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
		InitAllRule();
		GenerateCppProject();
	}

	public void GenerateCppProject()
	{
		InitAllRule();
		
		var slnResult = ServiceContext.Instance.Create<ISlnGenerator>(Name, ProjectRoot);
		if (!slnResult)
		{
			throw new Exception("cannot parse sln generator");
		}
		
		var slnGenerator = slnResult.Value;
		slnGenerator.GenerateOrGetVCProj(this, CppProjectOutput);
		slnGenerator.FlushProjectsAndSln();
	}

	public void Build(string? targetName = null)
	{
		Build(targetName, null);
	}
	
	public void Build(string? targetName, IBuildConfigProvider? configProvider = null)
	{
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

	public void Clean()
	{
		IntermediaFolder.DeleteIfExists(DeleteMode.Normal);
	}

	public void ReBuild(string? targetName = null)
	{
		Clean();
		Build(targetName);
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
		if (NeedReBuildRuleAssembly())
		{
			BuildRuleAssembly();
		}
		
		if (TargetRules.Count != 0 || ModuleRules.Count != 0)
		{
			return;
		}
		
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

	private void Build(CppBuilder builder, ITargetInterface targetRule)
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

	public string Name { get; }
	public NPath ProjectRoot { get; }

	public NPath IntermediaFolder => ProjectRoot.Combine("Intermedia");
	public NPath SourceFolder => ProjectRoot.Combine("Source");
	
	public Dictionary<string, ITargetInterface> TargetRules { get; } = new();
	public Dictionary<string, IModuleInterface> ModuleRules { get; } = new();

	private Dictionary<string, NPath> TargetRulePaths { get; } = new();
	private Dictionary<string, NPath> ModuleRulePaths { get; } = new();
	
	private IAssemblyCompileUnit BuildRuleCompileUnit { get; set; }
	private NPath CppBuildRuleProjectOutput => IntermediaFolder.Combine("CppBuildRule/Project");
	private NPath CppProjectOutput => IntermediaFolder.Combine("CppProject");
	private NPath CppBuildRuleBinaryOutput => IntermediaFolder.Combine("CppBuildRule/Binary");
	private NPath CppBuildRuleDllPath => CppBuildRuleBinaryOutput.Combine($"{BuildRuleCompileUnit.FileName}.dll");

}