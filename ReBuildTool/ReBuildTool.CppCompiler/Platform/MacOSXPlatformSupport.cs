using ReBuildTool.Service.Global;

namespace ReBuildTool.ToolChain;

public class MacOSXPlatformSupport : IPlatformSupport
{
	public override bool Supports(RuntimePlatform platform)
	{
		return platform is MacOSXRuntimePlatform;
	}

	public override IToolChain MakeCppToolChain(Architecture architecture, BuildConfiguration buildConfiguration)
	{
		return new MacOSXClangToolchain(buildConfiguration, architecture);
	}
}