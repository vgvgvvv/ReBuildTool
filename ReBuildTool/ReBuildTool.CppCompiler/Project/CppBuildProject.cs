using NiceIO;

namespace ReBuildTool.ToolChain;

public interface ICppSourceProvider
{
	Dictionary<string, TargetRule> TargetRules { get; }
	Dictionary<string, ModuleRule> ModuleRules { get; }
}

public class CppBuildProject : ICppSourceProvider
{
	public static CppBuildProject Create()
	{
		return new CppBuildProject();
	}

	private CppBuildProject()
	{
		
	}

	public CppBuildProject Parse(string workDirectory)
	{
		var targetFiles = Directory.GetFiles(workDirectory, "*.Target.cs");
		var moduleFiles = Directory.GetFiles(workDirectory, "*.Module.cs");
		var extraFiles = Directory.GetFiles(workDirectory, "*.Extension.cs");
		
		return this;
	}
	
	
	

	public void Setup()
	{
		
	}

	public void Build()
	{
		
		
	}

	public Dictionary<string, TargetRule> TargetRules { get; } = new();
	public Dictionary<string, ModuleRule> ModuleRules { get; } = new();
}