using NiceIO;

using ReBuildTool.CppCompiler.Standalone;

namespace ReBuildTool.ToolChain;

public class BuildOptions
{

	public static BuildOptions CreateDefault(IPlatformSupport platformSupport)
	{
		var option = new BuildOptions();

		var archName = CppCompilerArgs.Get().TargetArch.Value;
		if (string.IsNullOrEmpty(archName))
		{
			if (platformSupport is WindowsPlatformSupport)
			{
				option.Architecture = new x64Architecture();
			}
			else if (platformSupport is iOSPlatformSupport)
			{
				option.Architecture = new ARM64Architecture();
			}
			else if (platformSupport is LinuxPlatformSupport)
			{
				option.Architecture = new x64Architecture();
			}
			else if (platformSupport is MacOSXPlatformSupport)
			{
				option.Architecture = new ARM64Architecture();
			}
			else if (platformSupport is AndroidPlatformSupport)
			{
				option.Architecture = new ARM64Architecture();
			}
			else
			{
				throw new NotSupportedException("not supported platform");
			}
		} 
		else
		{
			if (archName == "x86")
			{
				option.Architecture = new x86Architecture();
			}
			else if (archName == "x64")
			{
				option.Architecture = new x64Architecture();
			}
			else if (archName == "arm32")
			{
				option.Architecture = new ARMv7Architecture();
			} 
			else if (archName == "arm64")
			{
				option.Architecture = new ARM64Architecture();
			}
			else
			{
				throw new NotSupportedException($"not supported arch {archName}, only support : x86, x64, arm32, arm64");
			}
		}
		
		return option;
	}

	private BuildOptions()
	{
	}
	
	public BuildOptions(BuildConfiguration configuration, Architecture arch)
	{
		Architecture = arch;
		Configuration = configuration;
	}

	public Architecture Architecture { get; private set; } = new x64Architecture();
	public BuildConfiguration Configuration { get; private set; } = BuildConfiguration.Debug;
	
	public List<NPath> CustomIncludeDirectories { get; } = new List<NPath>();
	
	public List<string> CustomDefines { get; } = new List<string>();
	
	public List<string> CustomCompileFlags { get; } = new List<string>();

	public List<string> CustomLinkFlags { get; } = new List<string>();
	
	public List<string> CustomStaticLibraries { get; } = new List<string>();
	
	public List<string> CustomDynamicLibraries { get; } = new List<string>();
	
	public List<NPath> CustomLibraryDirectories { get; } = new List<NPath>();

	public List<string> CustomArchiveFlags { get; } = new List<string>();
}