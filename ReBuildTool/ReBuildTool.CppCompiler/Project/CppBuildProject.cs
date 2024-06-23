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
		
		SimpleAssemblyCompileUnit targetCompileUnit = new();
		targetCompileUnit.SourceFiles.AddRange(targetFiles.Select(f => f.ToNPath()));
		targetCompileUnit.SourceFiles.AddRange(moduleFiles.Select(f => f.ToNPath()));
		targetCompileUnit.SourceFiles.AddRange(extraFiles.Select(f => f.ToNPath()));
		targetCompileUnit.ReferenceDlls.Add(Assembly.GetAssembly(typeof(CppBuildProject))!.Location.ToNPath());
		targetCompileUnit.FileName = "CompileRules";
		
		// TODO: generate cs projects
		return this;
	}

	public void Setup()
	{
		// generate dll && do init functions
	}

	public void Build()
	{	
		// do build & build hooks	
	}

	public Dictionary<string, TargetRule> TargetRules { get; } = new();
	public Dictionary<string, ModuleRule> ModuleRules { get; } = new();
}