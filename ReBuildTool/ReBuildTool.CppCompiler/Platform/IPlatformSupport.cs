using ReBuildTool.CppCompiler;
using ReBuildTool.Service.Global;

namespace ReBuildTool.ToolChain;

public enum PlatformSupportType
{
	None,
	Windows,
	iOS,
	Linux,
	MacOSX,
	Android
}

public abstract class IPlatformSupport
{
	public static Dictionary<PlatformSupportType, IPlatformSupport> SupprtedPlatforms { get; } = new()
	{
		{ PlatformSupportType.Windows, new WindowsPlatformSupport() },
		{ PlatformSupportType.iOS, new iOSPlatformSupport() },
		{ PlatformSupportType.Linux, new LinuxPlatformSupport() },
		{ PlatformSupportType.MacOSX, new MacOSXPlatformSupport() },
		{ PlatformSupportType.Android, new AndroidPlatformSupport() }
	};

	public static PlatformSupportType CurrentTargetPlatform
	{
		get
		{
			var targetPlatform = CppCompilerArgs.Get().TargetPlatform.Value;
			if (targetPlatform == PlatformSupportType.None)
			{
				if (PlatformHelper.IsWindows())
				{
					targetPlatform = PlatformSupportType.Windows;
				}
				else if (PlatformHelper.IsLinux())
				{
					targetPlatform = PlatformSupportType.Linux;
				}
				else if (PlatformHelper.IsOSX())
				{
					targetPlatform = PlatformSupportType.MacOSX;
				}
				else
				{
					throw new PlatformNotSupportedException();	
				}
			}

			return targetPlatform;
		}
	}

	public static IPlatformSupport CurrentTargetPlatformSupport => SupprtedPlatforms[CurrentTargetPlatform];
	
	public abstract bool Supports(RuntimePlatform platform);
	
	public abstract IToolChain MakeCppToolChain(Architecture architecture, BuildConfiguration buildConfiguration);

}