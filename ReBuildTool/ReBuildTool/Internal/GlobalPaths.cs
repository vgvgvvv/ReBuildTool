using NiceIO;

namespace ReBuildTool.Internal;

public class GlobalPaths
{
	public static NPath ProjectRoot => CommonCommandGroup.Get().ProjectRoot.ToNPath();
	
	public static NPath IntermediaPath => ProjectRoot.Combine("Intermedia");
}