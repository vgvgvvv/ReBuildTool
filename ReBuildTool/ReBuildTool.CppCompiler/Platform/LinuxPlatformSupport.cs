using NiceIO;
using ReBuildTool.Service.Global;

namespace ReBuildTool.ToolChain;

public class LinuxPlatformSupport : IPlatformSupport
{
	
	public override bool Supports(RuntimePlatform platform)
	{
		return platform is LinuxRuntimePlatform;
	}

	// TODO: support clang toolchain :
	// new LinuxClangToolchain(buildConfiguration, architecture);
	public override IToolChain MakeCppToolChain(Architecture architecture, BuildConfiguration buildConfiguration)
	{
		return new GccToolChain(buildConfiguration, architecture);
	}
}