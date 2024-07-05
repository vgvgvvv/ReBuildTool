using NiceIO;
using ReBuildTool.Service.CommandGroup;

namespace ReBuildTool.Service.Global;

public class GlobalPaths
{
	public static NPath ProjectRoot => GlobalCmd.CommonCommand.GetProjectRoot();
	
	public static NPath SourceRoot => GlobalCmd.CommonCommand.GetSourceRoot();
	
	public static NPath IntermediaPath => GlobalCmd.CommonCommand.GetIntermediaPath();
	
	public static NPath ToolRoot => GlobalCmd.CommonCommand.GetToolRoot();
	
	public static NPath ScriptRoot => GlobalCmd.CommonCommand.GetScriptRoot();
	
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