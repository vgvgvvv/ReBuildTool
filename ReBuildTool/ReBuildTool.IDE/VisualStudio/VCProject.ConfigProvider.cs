using ReBuildTool.Service.Global;
using ReBuildTool.ToolChain;

namespace ReBuildTool.IDE.VisualStudio;

public partial class VCProject
{
	internal class VCProjectConfigProvider : IBuildConfigProvider
	{
		public VCProjectConfigProvider()
		{
			if (PlatformHelper.IsWindows())
			{
				platform = new WindowsPlatformSupport();
			}
			else if (PlatformHelper.IsOSX())
			{
				platform = new MacOSXPlatformSupport();
			}
			else if (PlatformHelper.IsLinux())
			{
				platform = new LinuxPlatformSupport();
			}
			else
			{
				throw new NotSupportedException("not supported platform");
			}
			options = BuildOptions.CreateDefault(platform);
			ToolChain = platform.MakeCppToolChain(options.Architecture, options.Configuration);
		}
		
		public IPlatformSupport GetBuildPlatform()
		{
			return platform;
		}

		public BuildOptions GetBuildOptions()
		{
			return options;
		}

		public IToolChain ToolChain { get; }
		
		private BuildOptions options { get; } 
		private IPlatformSupport platform { get; } 
		
	}
}
