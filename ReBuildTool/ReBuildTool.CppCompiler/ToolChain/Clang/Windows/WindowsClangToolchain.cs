using NiceIO;
using ReBuildTool.ToolChain.SDK;

namespace ReBuildTool.ToolChain.Windows;

public partial class WindowsClangToolchain : ClangToolChain
{
	public WindowsClangToolchain(BuildConfiguration configuration, Architecture arch, NPath llvmSDKLocation) : base(configuration, arch)
	{
		ClangSdk = new WindowsClangSDK(llvmSDKLocation);
	}

	protected override ClangSDK ClangSdk { get; }
	
	public override IEnumerable<ICppLibrary> CppLibraries()
	{
		foreach (var cppLibrary in ClangSdk.GetCppLibs(Arch))
		{
			yield return cppLibrary;
		}
	}

	public override string ObjectExtension => ".obj";
	public override string ExecutableExtension => ".exe";
	public override string StaticLibraryExtension => ".lib";
	public override string DynamicLibraryExtension => ".dll";
	
	// use msvc toolchain compile arg builder directly
	
	public override ICompileArgsBuilder MakeCompileArgsBuilder()
	{
		return new MSVCCompileArgsBuilder();
	}

	public override ILinkArgsBuilder MakeLinkArgsBuilder()
	{
		return new MSVCLinkArgsBuilder();
	}

	public override IArchiveArgsBuilder MakeArchiveArgsBuilder()
	{
		return new MSVCArchiveArgsBuilder();
	}
}