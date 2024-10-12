using ReBuildTool.ToolChain;

namespace ReBuildTool.Service.CompileService.HeaderTool;

public class HeaderToolPluginSupport : BaseCppTargetCompilePlugin
{
	public override void PreCompile(CppTargetRule targetRule, CppBuilder builder)
	{
		base.PreCompile(targetRule, builder);
	}
	
	public override void PostCompile(CppTargetRule targetRule, CppBuilder builder)
	{
		base.PostCompile(targetRule, builder);
	}
}