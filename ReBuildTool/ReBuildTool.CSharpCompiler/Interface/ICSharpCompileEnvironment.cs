using NiceIO;
using ReBuildTool.Service.CompileService;

namespace ReBuildTool.CSharpCompiler;

public abstract class CSharpCompileEnvironmentBase : ICSharpCompileEnvironment
{
	public abstract CSharpCompileConfiguration Configuration { get; }
	public abstract bool AllowUnsafe { get; set; }
	public abstract List<string> Definitions { get; }
	public abstract List<string> AutoReferencedUnitNames { get; }
	
	public abstract NPath CsharpBuildRoot { get; }
}