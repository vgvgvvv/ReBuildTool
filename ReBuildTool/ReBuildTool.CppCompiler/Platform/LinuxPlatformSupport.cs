using NiceIO;

using ReBuildTool.CppCompiler.Standalone;
using ReBuildTool.Service.Global;

namespace ReBuildTool.ToolChain;

public class LinuxPlatformSupport : IPlatformSupport
{
	
	public override bool Supports(RuntimePlatform platform)
	{
		return platform is LinuxRuntimePlatform;
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
		return new GccToolChain(buildConfiguration, architecture);
	}
}