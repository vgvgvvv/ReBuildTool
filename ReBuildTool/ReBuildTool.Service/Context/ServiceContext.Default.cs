using System.Reflection;
using NiceIO;
using ReBuildTool.Service.CompileService;
using ReBuildTool.Service.IDEService;
using ReBuildTool.Service.IDEService.CMake;
using ReBuildTool.Service.IDEService.VisualStudio;

namespace ReBuildTool.Service.Context;

public partial class ServiceContext
{
	public void InitByDefault()
	{
		var ideDll = FindAssembly("ReBuildTool.IDE");
		RegisterType<ISlnGenerator>(ideDll, "ReBuildTool.IDE.VisualStudio.SlnGenerator");
		RegisterType<ICMakeGenerator>(ideDll, "ReBuildTool.IDE.CMake.CMakeGenerator");
		RegisterType<IGenerateIDEProjService>(ideDll, "ReBuildTool.IDE.Common.IDEProjectGenerator");
		
		var cppDll = FindAssembly("ReBuildTool.CppCompiler");
		RegisterType<ICppProject>(cppDll, "ReBuildTool.ToolChain.Project.CppBuildProject");
		
		var csharpDll = FindAssembly("ReBuildTool.CSharpCompiler");
		RegisterType<IAssemblyCompileUnit>(csharpDll, "ReBuildTool.CSharpCompiler.SimpleAssemblyCompileUnit");
		RegisterService<ICSharpCompilerService>(csharpDll, "ReBuildTool.CSharpCompiler.SimpleCompiler");
		
		var iniDll = FindAssembly("ReBuildTool.Ini");
		RegisterType<IIniProject>(iniDll, "ReBuildTool.IniProject.ModuleProject");
	}

	private Assembly FindAssembly(string name)
	{
		var binPath = Assembly.GetAssembly(typeof(ServiceContext)).Location.ToNPath().Parent;
		var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(ass => ass.GetName().Name == name);
		if (assembly == null)
		{
			assembly = Assembly.LoadFile(binPath.Combine($"{name}.dll"));
		}

		return assembly;
	}
}