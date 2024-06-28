using NiceIO;
using ReBuildTool.Service.CompileService;
using ResetCore.Common;

namespace ReBuildTool.CSharpCompiler;

public class SimpleCompileEnvironment : CSharpCompileEnvironmentBase
{
	public override CSharpCompileConfiguration Configuration => CSharpCompileArgs.Get().CSCompileConfig;
	public override bool AllowUnsafe { get; set; }
	public override List<string> Definitions { get; } = new List<string>();
	public override List<string> AutoReferencedUnitNames { get; } = new List<string>();
	public override NPath CsharpBuildRoot => CSharpCompileArgs.Get().CSharpBuildRoot.ToNPath();
}