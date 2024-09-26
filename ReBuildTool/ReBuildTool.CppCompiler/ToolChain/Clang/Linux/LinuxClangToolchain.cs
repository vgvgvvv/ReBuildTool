using NiceIO;
using ReBuildTool.ToolChain.SDK;

namespace ReBuildTool.ToolChain;

public partial class LinuxClangToolchain : ClangToolChain
{
	public LinuxClangToolchain(BuildConfiguration configuration, Architecture arch, NPath clangSdkLocation) : base(configuration, arch)
	{
		ClangSdk = new LinuxClangSDK(clangSdkLocation);
	}
	
	protected override ClangSDK ClangSdk { get; }
	
	public LinuxClangSDK LinuxSdk => ClangSdk as LinuxClangSDK;
	public override IEnumerable<ICppLibrary> CppLibraries()
	{
		foreach (var cppLibrary in ClangSdk.GetCppLibs(Arch))
		{
			yield return cppLibrary;
		}
	}

	public override string ObjectExtension => ".o";
	public override string ExecutableExtension => string.Empty;
	public override string StaticLibraryExtension => ".a";
	public override string DynamicLibraryExtension => ".so";
	public override string LibraryPrefix => "lib";
}