using NiceIO;

namespace UnityCompiler.Internal.Compile;


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

internal class DllCache
{
	public string Name;
	public NPath Location;
}

internal class CompileContext
{
	public string OutputPath;
	public List<IAssemblyCompileUnit> CompileUnits = new ();
	public Dictionary<string, DllCache> DllCache = new();
}

public abstract class ICSharpCompiler
{
	public void Compile(string outputPath, List<IAssemblyCompileUnit> compileUnits)
	{
		context = new CompileContext()
		{
			OutputPath = outputPath,
			CompileUnits = compileUnits
		};
		Compile(context);
	}

	public static ICSharpCompiler Default { get; } = new SimpleCompiler();

	internal abstract void Compile(CompileContext compileContext);

	internal CompileContext context { get; private set; } = new ();
}