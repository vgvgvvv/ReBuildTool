using ReBuildTool.ToolChain.SDK;

namespace ReBuildTool.ToolChain.Linux;

public class LinuxClangToolchain : ClangToolChain
{
	public LinuxClangToolchain(BuildConfiguration configuration, Architecture arch) : base(configuration, arch)
	{
	}
	
	protected override ClangSDK ClangSdk { get; }
	public override IEnumerable<ICppLibrary> CppLibraries()
	{
		throw new NotImplementedException();
	}

	public override string ObjectExtension { get; }
	public override string ExecutableExtension { get; }
	public override string StaticLibraryExtension { get; }
	public override string DynamicLibraryExtension { get; }
}