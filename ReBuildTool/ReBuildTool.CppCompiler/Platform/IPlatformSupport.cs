using ReBuildTool.CppCompiler.Standalone;
using ReBuildTool.Service.Global;

namespace ReBuildTool.ToolChain;

public enum PlatformSupportType
{
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

	public static PlatformSupportType CurrentTargetPlatform => CppCompilerArgs.Get().TargetPlatform;
	
	public static IPlatformSupport CurrentTargetPlatformSupport => SupprtedPlatforms[CurrentTargetPlatform];
	
	public abstract bool Supports(RuntimePlatform platform);
	
	public abstract IToolChain MakeCppToolChain(Architecture architecture, BuildConfiguration buildConfiguration);

}