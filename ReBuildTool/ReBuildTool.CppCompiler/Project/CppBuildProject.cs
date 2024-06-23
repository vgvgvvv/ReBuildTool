using System.Reflection;
using NiceIO;
using ReBuildTool.CSharpCompiler;

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

	public CppBuildProject Parse()
	{
		var targetFiles = ProjectRoot.Files("*.Target.cs", true);
		var moduleFiles = ProjectRoot.Files("*.Module.cs", true);
		var extraFiles = ProjectRoot.Files("*.Extension.cs", true);
		
		BuildRuleCompileUnit = new SimpleAssemblyCompileUnit();
		BuildRuleCompileUnit.SourceFiles.AddRange(targetFiles);
		BuildRuleCompileUnit.SourceFiles.AddRange(moduleFiles);
		BuildRuleCompileUnit.SourceFiles.AddRange(extraFiles);
		BuildRuleCompileUnit.ReferenceDlls.Add(Assembly.GetAssembly(typeof(CppBuildProject))!.Location.ToNPath());
		BuildRuleCompileUnit.FileName = "CompileRules";

		SlnGenerator slnGenerator = SlnGenerator.Create("CompileRules", ProjectRoot);
		NetCoreCSProj.GenerateOrGetCSProj(slnGenerator, BuildRuleCompileUnit, CompileEnvironment.Default,
			CppBuildRuleProjectOutput);

		slnGenerator.FlushProjectsAndSln();
		
		return this;
	}

	public void Setup()
	{
		// generate dll && do init functions
		ICSharpCompiler.Default.Compile(CppBuildRuleBinaryOutput, new List<IAssemblyCompileUnit>()
		{
			BuildRuleCompileUnit
		});
	}

	public void Build()
	{	
		// do build & build hooks	
	}
	
	public NPath ProjectRoot { get; }

	public NPath IntermediaFolder => ProjectRoot.Combine("Intermedia");
	
	public Dictionary<string, TargetRule> TargetRules { get; } = new();
	public Dictionary<string, ModuleRule> ModuleRules { get; } = new();
	
	private IAssemblyCompileUnit BuildRuleCompileUnit { get; set; }
	private NPath CppBuildRuleProjectOutput => IntermediaFolder.Combine("CppBuildRule/Project");
	private NPath CppBuildRuleBinaryOutput => IntermediaFolder.Combine("CppBuildRule/Binary");

}