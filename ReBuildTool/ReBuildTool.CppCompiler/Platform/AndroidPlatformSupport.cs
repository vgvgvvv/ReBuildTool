using ReBuildTool.Service.Global;

namespace ReBuildTool.ToolChain;

public class AndroidPlatformSupport : IPlatformSupport
{
	public override bool Supports(RuntimePlatform platform)
	{
		return platform is AndroidRuntimePlatform;
	}

	public override IToolChain MakeCppToolChain(Architecture architecture, BuildConfiguration buildConfiguration)
	{
		throw new NotImplementedException();
	}
}