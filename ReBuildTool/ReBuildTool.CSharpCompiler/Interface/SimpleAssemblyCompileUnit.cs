using NiceIO;
using ReBuildTool.Service.CompileService;

namespace ReBuildTool.CSharpCompiler;

public class SimpleAssemblyCompileUnit : IAssemblyCompileUnit
{
	public override string FileName { get; set; }
	public override CompileOutputType CompileType { get; set; }
	public override string TargetFrameworkVersion { get; set; }
	public override List<NPath> SourceFiles { get; } = new();
	public override List<string> Definitions { get; } = new();
	public override List<NPath> ReferenceDlls { get; } = new();
	public override List<IAssemblyCompileUnit> References { get; } = new();
	public override bool Unsafe { get; set; }
	public override bool TreatWarningsAsErrors { get; set; }
	public override string RootNamespace { get; set; }
}