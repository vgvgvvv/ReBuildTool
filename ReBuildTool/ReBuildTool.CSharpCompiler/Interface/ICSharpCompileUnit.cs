using NiceIO;
using ReBuildTool.Service.CompileService;

namespace ReBuildTool.CSharpCompiler;

public abstract class IAssemblyCompileUnitBase : IAssemblyCompileUnit
{
	public abstract string FileName { get; set; }
	
	public abstract CompileOutputType CompileType { get; set; }
	
	public abstract string TargetFrameworkVersion { get; set; }
	
	public abstract List<NPath> SourceFiles { get; }
	
	public abstract List<string> Definitions { get; }

	public abstract List<NPath> ReferenceDlls { get; }
	
	public abstract List<IAssemblyCompileUnit> References { get; }
	
	public abstract bool Unsafe { get; set; }

	public abstract bool TreatWarningsAsErrors { get; set; }
	
	public abstract string RootNamespace { get; set; }
}