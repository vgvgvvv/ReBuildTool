using NiceIO;
using ReBuildTool.Service.Global;
using ReBuildTool.ToolChain.SDK;

namespace ReBuildTool.ToolChain.Android;

public partial class AndroidClangToolchain : ClangToolChain
{
	public AndroidClangToolchain(BuildConfiguration configuration, Architecture arch, NPath ndkLocation) : base(configuration, arch)
	{
		ClangSdk = new NDKClangSDK(ndkLocation, PlatformHelper.GetBuildEnvironmentPlatform(), arch);
	}

	public override Dictionary<string, string> EnvVars()
	{
		var dict = base.EnvVars();
		string spliter = PlatformHelper.IsWindows() ? ";" : ":";
		var paths = dict["PATH"].Split(spliter).ToList();
		dict["PATH"] = string.Join(spliter, paths);
		return dict;
	}

	public override string ObjectExtension => ".o";
	public override string ExecutableExtension => "";
	public override string StaticLibraryExtension => ".a";
	public override string DynamicLibraryExtension => ".so";
	public override string LibraryPrefix => "lib";

	protected override ClangSDK ClangSdk { get; }
	
	protected NDKClangSDK NdkClangSdk => ClangSdk as NDKClangSDK;
	public override IEnumerable<ICppLibrary> CppLibraries()
	{
		foreach (var cppLibrary in ClangSdk.GetCppLibs(Arch))
		{
			yield return cppLibrary;
		}
	}
}