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
	
	[CmdLine("target architecture")]
	public CmdLineArg<string> TargetArch { get; set; }
	
	[CmdLine("use clang")]
	public CmdLineArg<bool> UseClang { get; set; } = CmdLineArg<bool>.FromObject(nameof(UseClang), false);
	
	[CmdLine("clang path")]
	public CmdLineArg<string> ClangPath { get; set; }
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