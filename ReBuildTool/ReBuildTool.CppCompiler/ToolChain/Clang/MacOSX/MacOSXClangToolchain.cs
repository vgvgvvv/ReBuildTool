using ReBuildTool.ToolChain.SDK;
using ReBuildTool.ToolChain.SDK.XCodeClang;

namespace ReBuildTool.ToolChain;

public partial class MacOSXClangToolchain : ClangToolChain
{
	public MacOSXClangToolchain(BuildConfiguration configuration, Architecture arch) : base(configuration, arch)
	{
		ClangSdk = new XCodeClangSDK();
	}
	
	protected override ClangSDK ClangSdk { get; }
	public override IEnumerable<ICppLibrary> CppLibraries()
	{
		throw new NotImplementedException();
	}

	public override string ObjectExtension => ".o";
	public override string ExecutableExtension => string.Empty;
	public override string StaticLibraryExtension => ".a";
	public override string DynamicLibraryExtension => ".dylib";
	
}