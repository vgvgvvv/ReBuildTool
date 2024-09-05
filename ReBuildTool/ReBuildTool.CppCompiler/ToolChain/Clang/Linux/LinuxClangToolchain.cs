using ReBuildTool.ToolChain.SDK;

namespace ReBuildTool.ToolChain.Linux;

public partial class LinuxClangToolchain : ClangToolChain
{
	public LinuxClangToolchain(BuildConfiguration configuration, Architecture arch) : base(configuration, arch)
	{
	}
	
	protected override ClangSDK ClangSdk { get; }
	public override IEnumerable<ICppLibrary> CppLibraries()
	{
		throw new NotImplementedException();
	}

	public override string ObjectExtension => ".o";
	public override string ExecutableExtension => string.Empty;
	public override string StaticLibraryExtension => ".a";
	public override string DynamicLibraryExtension => ".so";
}