using ReBuildTool.Service.Global;

namespace ReBuildTool.ToolChain;

public class WindowsPlatformSupport : IPlatformSupport
{
	public override bool Supports(RuntimePlatform platform)
	{
		return platform is WindowsDesktopRuntimePlatform;
	}

	public override IToolChain MakeCppToolChain(Architecture architecture, BuildConfiguration buildConfiguration)
	{
		return new MSVCToolChain(buildConfiguration, architecture);
	}
}