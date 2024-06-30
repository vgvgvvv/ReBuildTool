using NiceIO;
using ReBuildTool.Service.CompileService;
using ReBuildTool.Service.IDEService.CMake;
using ReCSharpCommon.Result;

namespace ReBuildTool.ToolChain.IDE.CMake;

public class CMakeLists : ICMakeLists
{
	public string name { get; }
	public NPath fullPath { get; }
	public bool FlushToFile()
	{
		throw new NotImplementedException();
	}
}

public class CMakeGenerator : ICMakeGenerator
{
	public string Name { get; }
	public string OutputPath { get; }
	public ICMakeLists GenerateCMakeProject(ICppSourceProviderInterface source, NPath output)
	{
		throw new NotImplementedException();
	}

	public bool FlushAllCMakeFile()
	{
		throw new NotImplementedException();
	}
}