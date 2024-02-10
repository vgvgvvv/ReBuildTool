using Bullseye;

namespace ReBuildTool.Internal.Lua;

public class LuaModule : LuaModuleBase, IBuildItem
{
	public LuaModule(string modulePath, ModuleProject owner) : base(modulePath, owner)
	{
	}
	
	public void SetupInitTargets(Targets targets, ref List<string> newTargets)
	{
	}

	public void SetupBuildTargets(Targets targets, ref List<string> newTargets)
	{
	}

	
}