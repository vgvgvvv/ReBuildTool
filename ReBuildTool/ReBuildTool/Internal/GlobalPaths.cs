using NiceIO;

namespace ReBuildTool.Internal;

public class GlobalPaths
{
	public static NPath ProjectRoot => CommonCommandGroup.Get().ProjectRoot.ToNPath();
	
	//{Root}/Binary/Platform/ReBuildTool/ReBuildTool.dll
	public static NPath ToolRoot => typeof(Program).Assembly.Location.ToNPath().Parent.Parent.Parent.Parent;
	
	public static NPath ScriptRoot => ProjectRoot.Combine("BuildScript");
	
	public static NPath IntermediaPath => ProjectRoot.Combine("Intermedia");
}