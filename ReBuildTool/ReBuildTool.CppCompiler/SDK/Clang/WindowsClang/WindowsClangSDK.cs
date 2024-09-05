using NiceIO;

namespace ReBuildTool.ToolChain.SDK;

public class WindowsClangSDK : ClangSDK
{
	public WindowsClangSDK(NPath root) : base(root)
	{
	}

	public override IEnumerable<ICppLibrary> GetCppLibs(Architecture arch)
	{
		var msvc =  MsvcSDK.FindLatestSDK()
			.SetArch(arch)
			.UseLastestVCPaths()
			.UseLatestWindowsKit();
		yield return msvc;
	}

	public override NPath GetCompiler()
	{
		return RootPath.Combine("bin/clang-cl.exe");
	}

	public override NPath GetLinker()
	{
		return RootPath.Combine("bin/clang-cl.exe");
	}

	public override NPath GetArchiver()
	{
		return RootPath.Combine("bin/clang-cl.exe");
	}
}