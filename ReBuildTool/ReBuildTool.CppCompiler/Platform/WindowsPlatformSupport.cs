using NiceIO;

using ReBuildTool.CppCompiler.Standalone;
using ReBuildTool.Service.Global;
using ReBuildTool.ToolChain.Windows;

namespace ReBuildTool.ToolChain;

public class WindowsPlatformSupport : IPlatformSupport
{
	public override bool Supports(RuntimePlatform platform)
	{
		return platform is WindowsDesktopRuntimePlatform;
	}

	public override IToolChain MakeCppToolChain(Architecture architecture, BuildConfiguration buildConfiguration)
	{
		var args = CppCompilerArgs.Get();
		if (args.UseClang)
		{
			var clangHome = args.ClangPath.Value;
			if (!string.IsNullOrEmpty(clangHome) && Directory.Exists(args.ClangPath))
			{
				return new LinuxClangToolchain(buildConfiguration, architecture, clangHome.ToNPath());
			}
			clangHome = Environment.GetEnvironmentVariable("CLANG_HOME");
			if (!string.IsNullOrEmpty(clangHome) && Directory.Exists(clangHome))
			{
				return new LinuxClangToolchain(buildConfiguration, architecture, clangHome.ToNPath());
			}
		}
		return new MSVCToolChain(buildConfiguration, architecture);
	}
}