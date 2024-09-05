using NiceIO;
using ReBuildTool.Service.Global;
using ReBuildTool.ToolChain.SDK;

namespace ReBuildTool.ToolChain.Android;

public class AndroidClangToolchain : ClangToolChain
{
	public AndroidClangToolchain(BuildConfiguration configuration, Architecture arch, NPath ndkLocation) : base(configuration, arch)
	{
		ClangSdk = new NDKClangSDK(ndkLocation, PlatformHelper.GetBuildEnvironmentPlatform());
	}

	public override string ObjectExtension => ".o";
	public override string ExecutableExtension => "";
	public override string StaticLibraryExtension => ".a";
	public override string DynamicLibraryExtension => ".so";

	protected override ClangSDK ClangSdk { get; }
	public override IEnumerable<ICppLibrary> CppLibraries()
	{
		foreach (var cppLibrary in ClangSdk.GetCppLibs(Arch))
		{
			yield return cppLibrary;
		}
	}
}