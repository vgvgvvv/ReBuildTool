using ReBuildTool.Service.Global;
using ReBuildTool.ToolChain.Wasm;

namespace ReBuildTool.ToolChain;
 
public class WasmPlatformSupport : IPlatformSupport
{

	public override bool Supports(RuntimePlatform platform)
	{
		return platform is WebRuntimePlatform;
	}

	public override IToolChain MakeCppToolChain(Architecture architecture, BuildConfiguration buildConfiguration)
	{
		return new WasmToolchain(buildConfiguration, architecture);
	}

}
