using NiceIO;
using ReBuildTool.CppCompiler;
using ReBuildTool.ToolChain.SDK;
using ReBuildTool.ToolChain.SDK.XCodeClang;

namespace ReBuildTool.ToolChain;

public class IOSClangToolchain : MacOSXClangToolchain
{
	public IOSClangToolchain(BuildConfiguration configuration, Architecture arch) : base(configuration, arch)
	{
		IPhoneXCodeSdk = new XCodeSDK("/Applications/Xcode.app".ToNPath(), XCodePlatformSDK.ApplePlatform.iPhoneOS);
	}

	public override IEnumerable<string> TargetPlatformArgs()
	{
		yield return "-target";
		var targetVersion = IOSCompileArgs.Get().IOSTargetVersion;
		yield return $"arm64-apple-ios{targetVersion.Value}";
	}

	protected override ClangSDK ClangSdk => IPhoneXCodeSdk;
	private XCodeSDK IPhoneXCodeSdk { get; }
	
}

public class IOSSimulatorClangToolchain : MacOSXClangToolchain
{
	public IOSSimulatorClangToolchain(BuildConfiguration configuration, Architecture arch) : base(configuration, arch)
	{
		IPhoneXCodeSdk = new XCodeSDK("/Applications/Xcode.app".ToNPath(), XCodePlatformSDK.ApplePlatform.iPhoneSimulator);
	}

	protected override ClangSDK ClangSdk => IPhoneXCodeSdk;
	private XCodeSDK IPhoneXCodeSdk { get; }
	
}