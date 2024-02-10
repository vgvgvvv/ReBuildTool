namespace ReBuildTool.Internal.Lua;

public class LuaModuleBase : ModuleBase
{
	public static readonly string StaticModuleFileExtension = ".module.lua"; 
	public static readonly string StaticTargetFileExtension = ".target.lua";
	public override string ModuleFileExtension => StaticModuleFileExtension; 
	public override string TargetFileExtension => StaticTargetFileExtension;
	
	public LuaModuleBase(string modulePath, ModuleProject owner) : base(modulePath, owner)
	{
	}
	
}