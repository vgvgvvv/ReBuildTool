using ReBuildTool.Common;
using ResetCore.Common;

namespace ReBuildTool.Actions;

public class Lua
{
	[ActionDefine("Lua.Run")]
	public static void RunLua(string luaPath, string args, string workDir)
	{
		Log.Info("Run Lua: ", luaPath, " ", args, " in ", workDir);
		// TODO:
	}
}