using ReBuildTool.Service.Global;

namespace ReBuildTool.ToolChain;

public class MaxOsPlatformSupport : IPlatformSupport
{
	public override bool Supports(RuntimePlatform platform)
	{
		throw new NotImplementedException();
	}

	public override IToolChain MakeCppToolChain(Architecture architecture, BuildConfiguration buildConfiguration)
	{
		throw new NotImplementedException();
	}
}