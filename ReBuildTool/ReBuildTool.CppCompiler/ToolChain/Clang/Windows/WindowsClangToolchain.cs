using ReBuildTool.ToolChain.SDK;

namespace ReBuildTool.ToolChain.Windows;

public class WindowsClangToolchain : ClangToolChain
{
	public WindowsClangToolchain(BuildConfiguration configuration, Architecture arch) : base(configuration, arch)
	{
	}

	protected override ClangSDK clangSdk { get; }
	
	public override string ObjectExtension => ".obj";
	public override string ExecutableExtension => ".exe";
	public override string StaticLibraryExtension => ".lib";
	public override string DynamicLibraryExtension => ".dll";
}