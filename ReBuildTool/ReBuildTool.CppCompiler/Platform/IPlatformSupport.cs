using ReBuildTool.Service.Global;

namespace ReBuildTool.ToolChain;

public abstract class IPlatformSupport
{
	public abstract bool Supports(RuntimePlatform platform);
	
	public abstract IToolChain MakeCppToolChain(Architecture architecture, BuildConfiguration buildConfiguration);

}