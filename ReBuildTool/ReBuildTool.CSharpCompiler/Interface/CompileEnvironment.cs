namespace ReBuildTool.CSharpCompiler;

public class CompileEnvironment
{
	public static CompileEnvironment Default { get; } = new();
	
	public bool AllowUnsafe { get; set; }
	public List<string> Definitions { get; } = new List<string>();
	public List<string> AutoReferencedUnitNames { get; } = new List<string>();
}