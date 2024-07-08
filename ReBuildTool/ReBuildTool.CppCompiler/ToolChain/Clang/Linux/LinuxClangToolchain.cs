using ReBuildTool.ToolChain.SDK;

namespace ReBuildTool.ToolChain.Linux;

public class LinuxClangToolchain : ClangToolChain
{
	public LinuxClangToolchain(BuildConfiguration configuration, Architecture arch) : base(configuration, arch)
	{
	}
	
	protected override ClangSDK clangSdk { get; }

	public override string ObjectExtension { get; }
	public override string ExecutableExtension { get; }
	public override string StaticLibraryExtension { get; }
	public override string DynamicLibraryExtension { get; }
}