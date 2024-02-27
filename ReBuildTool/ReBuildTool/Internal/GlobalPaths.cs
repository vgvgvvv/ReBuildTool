using NiceIO;

namespace ReBuildTool.Internal;

public class GlobalPaths
{
	public static NPath ProjectRoot => CommonCommandGroup.Get().ProjectRoot.ToNPath();
	
	//{Root}/Binary/Platform/ReBuildTool/ReBuildTool.dll
	public static NPath ToolRoot => typeof(Program).Assembly.Location.ToNPath().Parent.Parent.Parent.Parent;
	
	public static NPath ScriptRoot => ToolRoot.Combine("BuildScript");
	
	public static NPath IntermediaPath => ProjectRoot.Combine("Intermedia");

	private static string? _reBuildToolHome = null;
	public static NPath ReBuildToolHome
	{
		get
		{
			if(_reBuildToolHome == null)
			{
				var rbtHome = Environment.GetEnvironmentVariable("RBT_HOME", EnvironmentVariableTarget.User);
				if (rbtHome != null)
				{
					_reBuildToolHome = rbtHome;
					return _reBuildToolHome.ToNPath();
				}
				rbtHome = Environment.GetEnvironmentVariable("RBT_HOME", EnvironmentVariableTarget.Machine);
				if (rbtHome != null)
				{
					_reBuildToolHome = rbtHome;
					return _reBuildToolHome.ToNPath();
				}
				// default as 
				rbtHome = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".rbt");
				_reBuildToolHome = rbtHome;
				return _reBuildToolHome.ToNPath();
			}

			return _reBuildToolHome.ToNPath();
		}
	}
}