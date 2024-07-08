using ReBuildTool.Service.Global;

namespace ReBuildTool.ToolChain;

public class LinuxPlatformSupport : IPlatformSupport
{
	public override bool Supports(RuntimePlatform platform)
	{
		return platform is LinuxRuntimePlatform;
	}

	public override IToolChain MakeCppToolChain(Architecture architecture, BuildConfiguration buildConfiguration)
	{
		throw new NotImplementedException();
	}
}