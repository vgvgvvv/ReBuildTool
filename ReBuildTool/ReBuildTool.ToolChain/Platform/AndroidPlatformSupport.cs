using ReBuildTool.Common;

namespace ReBuildTool.ToolChain;

public class AndroidPlatformSupport : IPlatformSupport
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