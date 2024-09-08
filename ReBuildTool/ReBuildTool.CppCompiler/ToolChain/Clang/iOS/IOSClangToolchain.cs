namespace ReBuildTool.ToolChain;

public class IOSClangToolchain : MacOSXClangToolchain
{
	public IOSClangToolchain(BuildConfiguration configuration, Architecture arch) : base(configuration, arch)
	{
	}
}