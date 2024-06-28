using NiceIO;
using ReBuildTool.Service.Context;

namespace ReBuildTool.Service.CompileService;

public enum CompileOutputType
{
	Library,
	Exe
}

public abstract class IAssemblyCompileUnit
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

public abstract class ICSharpCompileEnvironment
{
	public abstract bool AllowUnsafe { get; set; }
	public abstract List<string> Definitions { get; }
	public abstract List<string> AutoReferencedUnitNames { get; }
	
	public abstract NPath CsharpBuildRoot { get; }
}

public interface ICSharpCompiler : IService
{
	public IAssemblyCompileUnit CreateAssemblyUnit();
	public void Compile(string outputPath, List<IAssemblyCompileUnit> compileUnits);
	
	public ICSharpCompileEnvironment DefaultEnvironment { get; }
}