using NiceIO;
using ReBuildTool.Service.CompileService;
using ReBuildTool.Service.Context;

namespace ReBuildTool.Service.IDEService.VisualStudio;

public interface ISlnSubProject
{
	string name { get; }
	Guid guid { get; }
	NPath fullPath { get; }
	
	void FlushToFile();
}

public interface ISlnGenerator : IProvideByService
{
	bool GetSubProj(string name, out ISlnSubProject? outProj);
	
	bool FlushProjectsAndSln();
	
	public string Name { get; }

	public string OutputPath { get; }

	public ISlnSubProject GenerateOrGetNetCoreCSProj(IAssemblyCompileUnit unit,
		ICSharpCompileEnvironment env, NPath output);

	public ISlnSubProject GenerateOrGetNetFrameworkCSProj(IAssemblyCompileUnit unit,
		ICSharpCompileEnvironment env, NPath output);
	
}

