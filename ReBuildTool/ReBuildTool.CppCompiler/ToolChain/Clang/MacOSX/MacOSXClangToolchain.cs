using NiceIO;
using ReBuildTool.ToolChain.SDK;
using ReBuildTool.ToolChain.SDK.XCodeClang;

namespace ReBuildTool.ToolChain;

public partial class MacOSXClangToolchain : ClangToolChain
{
	public MacOSXClangToolchain(BuildConfiguration configuration, Architecture arch) : base(configuration, arch)
	{
		ClangSdk = new XCodeSDK("/Applications/Xcode.app".ToNPath(), XCodePlatformSDK.ApplePlatform.MacOSX);
	}
	
	protected override ClangSDK ClangSdk { get; }
	protected XCodeSDK XCodeSdk => ClangSdk as XCodeSDK;
	public override IEnumerable<ICppLibrary> CppLibraries()
	{
		foreach (var cppLibrary in XCodeSdk.GetCppLibs(Arch))
		{
			yield return cppLibrary;
		}
	}

	public override string ObjectExtension => ".o";
	public override string ExecutableExtension => string.Empty;
	public override string StaticLibraryExtension => ".a";
	public override string DynamicLibraryExtension => ".dylib";
	
}