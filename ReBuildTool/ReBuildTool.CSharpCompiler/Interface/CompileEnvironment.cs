namespace ReBuildTool.CSharpCompiler;

public class CompileEnvironment
{
	public bool AllowUnsafe { get; set; }
	public List<string> Definitions { get; } = new List<string>();
	public List<string> AutoReferencedUnitNames { get; } = new List<string>();
}