using System.Reflection;
using ReBuildTool.Service.CompileService;
using ReBuildTool.Service.IDEService.VisualStudio;

namespace ReBuildTool.Service.Context;

public partial class ServiceContext
{
	public void InitByDefault()
	{
		var ideDll = Assembly.LoadFile("ReBuildTool.IDE.dll");
		RegisterType<ISlnGenerator>(ideDll, "ReBuildTool.IDE.VisualStudio.SlnGenerator");
		
		var cppDll = Assembly.LoadFile("ReBuildTool.CppCompiler.dll");
		
		var csharpDll = Assembly.LoadFile("ReBuildTool.CSharpCompile.dll");
		RegisterType<IAssemblyCompileUnit>(csharpDll, "ReBuildTool.CSharpCompiler.SimpleAssemblyCompileUnit");
		RegisterService<ICSharpCompiler>(csharpDll, "ReBuildTool.CSharpCompiler.SimpleCompiler");
		
	}
}