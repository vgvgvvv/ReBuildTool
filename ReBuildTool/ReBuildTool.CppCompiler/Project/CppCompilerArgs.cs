using ReBuildTool.Service.CommandGroup;
using ReBuildTool.ToolChain;
using ResetCore.Common;

namespace ReBuildTool.CppCompiler;

public class CppCompilerArgs : CommandLineArgGroup<CppCompilerArgs>, ICommonCommandGroup
{
	[CmdLine("root of project, work directory as default")]
	public CmdLineArg<string> ProjectRoot { get; set; } = CmdLineArg<string>.FromObject(nameof(ProjectRoot), Environment.CurrentDirectory);

	[CmdLine("run mode: Init | Build", true)]
	public CmdLineArg<RunMode> Mode { get; set; }
	
	[CmdLine("build target")] 
	public CmdLineArg<string> Target { get; set; }

	[CmdLine("dry run for test")] 
	public CmdLineArg<bool> RunDry { get; set; } = CmdLineArg<bool>.FromObject(nameof(RunDry), false);

	[CmdLine("targetPlatform")]
	public CmdLineArg<PlatformSupportType> TargetPlatform { get; set; } =
		CmdLineArg<PlatformSupportType>.FromObject(nameof(TargetPlatform), PlatformSupportType.None);
	
	[CmdLine("target architecture : x86 | x64 | arm32 | arm64")]
	public CmdLineArg<string> TargetArch { get; set; }
	
	[CmdLine("build configuration : Debug | Release | ReleasePlus | ReleaseSize")]
	public CmdLineArg<BuildConfiguration> BuildConfig { get; set; } = CmdLineArg<BuildConfiguration>.FromObject(nameof(BuildConfiguration), BuildConfiguration.Debug);
	
	[CmdLine("use clang")]
	public CmdLineArg<bool> UseClang { get; set; } = CmdLineArg<bool>.FromObject(nameof(UseClang), false);
	
	[CmdLine("clang path")]
	public CmdLineArg<string> ClangPath { get; set; }
	
	[CmdLine("custom include directories")]
	public CmdLineArg<List<string>> CustomIncludeDirs { get; set; }
	
	[CmdLine("custom defines")]
	public CmdLineArg<List<string>> CustomDefines { get; set; }
	
	[CmdLine("custom compile flags")]
	public CmdLineArg<List<string>> CustomCompileFlags { get; set; }
	
	[CmdLine("custom link flags")]
	public CmdLineArg<List<string>> CustomLinkFlags { get; set; }
	
	[CmdLine("custom static libraries")]
	public CmdLineArg<List<string>> CustomStaticLibraries { get; set; }
	
	[CmdLine("custom dynamic libraries")]
	public CmdLineArg<List<string>> CustomDynamicLibraries { get; set; }
	
	[CmdLine("custom library directories")]
	public CmdLineArg<List<string>> CustomLibraryDirectories { get; set; }
	
	[CmdLine("custom archive flags")]
	public CmdLineArg<List<string>> CustomArchiveFlags { get; set; }
	
	[CmdLine("cpp compile plugins")]
	public CmdLineArg<List<string>> CppCompilePlugins { get; set; }
}

public class AndroidCompilerArgs : CommandLineArgGroup<AndroidCompilerArgs>
{
	[CmdLine("location of android ndk root")]
	public CmdLineArg<string> NDKRoot { get; set; }
	
	[CmdLine("location of android sdk root")]
	public CmdLineArg<string> SDKRoot { get; set; }
	
	[CmdLine("ndk toolchain version to use")]
	public CmdLineArg<int> NDKTargetVersion { get; set; } = CmdLineArg<int>.FromObject(nameof(NDKTargetVersion), 25);
}