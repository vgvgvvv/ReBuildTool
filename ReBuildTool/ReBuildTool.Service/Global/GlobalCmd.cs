using ReBuildTool.Service.CommandGroup;
using ResetCore.Common;

namespace ReBuildTool.Service.Global;

public static class GlobalCmd
{
	static GlobalCmd()
	{
		CommonCommand = CmdParser.Get<ICommonCommandGroup>();
	}
	
	public static ICommonCommandGroup CommonCommand { get; }
}