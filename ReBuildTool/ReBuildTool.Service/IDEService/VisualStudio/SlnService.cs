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
	
	string Name { get; }

	string OutputPath { get; }

	ISlnSubProject GenerateOrGetNetCoreCSProj(IAssemblyCompileUnit unit,
		ICSharpCompileEnvironment env, NPath output);
	
	ISlnSubProject GenerateOrGetNetFrameworkCSProj(IAssemblyCompileUnit unit,
		ICSharpCompileEnvironment env, NPath output);

	ISlnSubProject GenerateOrGetVCProj(ICppSourceProviderInterface source, NPath output);
}

