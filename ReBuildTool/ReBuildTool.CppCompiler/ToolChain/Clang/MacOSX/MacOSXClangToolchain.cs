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
	protected XCodeClangSDK XCodeClangSdk => ClangSdk as XCodeClangSDK;
	public override IEnumerable<ICppLibrary> CppLibraries()
	{
		foreach (var cppLibrary in XCodeClangSdk.GetCppLibs(Arch))
		{
			yield return cppLibrary;
		}
	}

	public override string ObjectExtension => ".o";
	public override string ExecutableExtension => string.Empty;
	public override string StaticLibraryExtension => ".a";
	public override string DynamicLibraryExtension => ".dylib";
	
}