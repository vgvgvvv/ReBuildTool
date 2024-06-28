using NiceIO;
using ReBuildTool.Service.CompileService;

namespace ReBuildTool.CSharpCompiler;


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
	public ICSharpCompileEnvironment Env;
}

public abstract class ICSharpCompilerBase : ICSharpCompilerService
{
	public IAssemblyCompileUnit CreateAssemblyUnit()
	{
		return new SimpleAssemblyCompileUnit();
	}

	public void Compile(string outputPath, List<IAssemblyCompileUnit> compileUnits, ICSharpCompileEnvironment env)
	{
		context = new CompileContext()
		{
			OutputPath = outputPath,
			CompileUnits = compileUnits,
			Env = env
		};
		Compile(context);
	}

	public ICSharpCompileEnvironment DefaultEnvironment { get; } = new SimpleCompileEnvironment();

	internal abstract void Compile(CompileContext compileContext);

	internal CompileContext context { get; private set; } = new ();
}