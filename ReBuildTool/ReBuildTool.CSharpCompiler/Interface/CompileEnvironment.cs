using NiceIO;
using ReBuildTool.Service.CompileService;

namespace ReBuildTool.CSharpCompiler;

public class SimpleCompileEnvironment : ICSharpCompileEnvironment
{
	public override bool AllowUnsafe { get; set; }
	public override List<string> Definitions { get; } = new List<string>();
	public override List<string> AutoReferencedUnitNames { get; } = new List<string>();
	public override NPath CsharpBuildRoot => CSharpCompileArgs.Get().CSharpBuildRoot.ToNPath();
}