using NiceIO;
using ReBuildTool.Service.Context;

namespace ReBuildTool.Service.CompileService;

public enum CompileOutputType
{
	Library,
	Exe
}

public interface IAssemblyCompileUnit
{
	public string FileName { get; set; }
	
	public CompileOutputType CompileType { get; set; }
	
	public string TargetFrameworkVersion { get; set; }
	
	public List<NPath> SourceFiles { get; }
	
	public List<string> Definitions { get; }

	public List<NPath> ReferenceDlls { get; }
	
	public List<IAssemblyCompileUnit> References { get; }
	
	public bool Unsafe { get; set; }

	public bool TreatWarningsAsErrors { get; set; }
	
	public string RootNamespace { get; set; }
}

public enum CSharpCompileConfiguration
{
	Debug,
	Release
}

public interface ICSharpCompileEnvironment
{
	public CSharpCompileConfiguration Configuration { get; }
	public bool AllowUnsafe { get; }
	public List<string> Definitions { get; }
	public List<string> AutoReferencedUnitNames { get; }

	
	public NPath CsharpBuildRoot { get; }
}

public interface ICSharpCompilerService : IService
{
	public IAssemblyCompileUnit CreateAssemblyUnit();

	public void Compile(string outputPath, List<IAssemblyCompileUnit> compileUnits,
		ICSharpCompileEnvironment env);
	
	public ICSharpCompileEnvironment DefaultEnvironment { get; }
}