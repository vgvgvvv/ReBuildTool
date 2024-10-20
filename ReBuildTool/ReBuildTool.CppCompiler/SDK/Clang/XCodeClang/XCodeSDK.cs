using NiceIO;
using ResetCore.Common;

namespace ReBuildTool.ToolChain.SDK.XCodeClang;

public abstract class XCodePlatformSDK
{
	public enum ApplePlatform
	{
		AppleTVOS,
		AppleTVSimulator,
		DriverKit,
		iPhoneOS,
		iPhoneSimulator,
		MacOSX,
		WatchOS,
		WatchSimulator
	}

	public XCodePlatformSDK(XCodeSDK owner)
	{
		Owner = owner;	
	}
	
	public abstract ApplePlatform Platform { get; }
	
	public XCodeSDK Owner { get; }

	public NPath PlatformRootPath => Owner.XCodePlatformLocation.Combine($"{Platform.ToString()}.platform");
	
	public NPath SDKPath => PlatformRootPath.Combine("Developer/SDKs").Combine($"{Platform.ToString()}.sdk");
}

public class iPhoneOSPlatformSDK : XCodePlatformSDK
{
	public iPhoneOSPlatformSDK(XCodeSDK owner) : base(owner)
	{
	}
	
	public override ApplePlatform Platform => ApplePlatform.iPhoneOS;
}

public class MacOSXPlatformSDK : XCodePlatformSDK
{
	public MacOSXPlatformSDK(XCodeSDK owner) : base(owner)
	{
	}
	
	public override ApplePlatform Platform => ApplePlatform.MacOSX;
}


public class XCodeSDK : ClangSDK
{
	
	
	public XCodeSDK(NPath xcodeLocation, XCodePlatformSDK.ApplePlatform platform) : base(xcodeLocation)
	{
		XCodeLocation = xcodeLocation;
		if (!XCodeLocation.Exists())
		{
			XCodeLocation = "/Applications/Xcode.app".ToNPath();
		}
		switch (platform)
		{
			case XCodePlatformSDK.ApplePlatform.AppleTVOS:
			case XCodePlatformSDK.ApplePlatform.AppleTVSimulator:
			case XCodePlatformSDK.ApplePlatform.DriverKit:
			case XCodePlatformSDK.ApplePlatform.WatchOS:
			case XCodePlatformSDK.ApplePlatform.WatchSimulator:
				throw new NotSupportedException($"Unsupported apple platform {platform}");
			case XCodePlatformSDK.ApplePlatform.iPhoneOS:
			case XCodePlatformSDK.ApplePlatform.iPhoneSimulator:
				PlatformSDK = new iPhoneOSPlatformSDK(this);
				break;
			case XCodePlatformSDK.ApplePlatform.MacOSX:
				PlatformSDK = new MacOSXPlatformSDK(this);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(platform), platform, null);
		}
	}
	
	public XCodePlatformSDK PlatformSDK { get; }

	public NPath XCodeLocation { get; }
	
	public NPath XCodeToolchainRoot => XCodeLocation.Combine("Contents/Developer/Toolchains/XcodeDefault.xctoolchain");
	public NPath XCodeToolchainIncludeLocation => XCodeToolchainRoot.Combine("usr/include");
	public NPath XCodeToolchainLibLocation => XCodeToolchainRoot.Combine("usr/lib");
	public NPath XCodeToolchainBinLocation => XCodeToolchainRoot.Combine("usr/bin");
	public NPath XCodeClangLocation => XCodeToolchainBinLocation.Combine("clang++");
	
	
	public NPath XCodePlatformLocation => XCodeLocation.Combine("Contents/Developer/Platforms");
	
	public NPath DefaultClangLocation { get; } = "/usr/bin/clang++".ToNPath();
	public NPath DefaultArToolLocation { get; } = "/usr/bin/ar".ToNPath();
	
	public override IEnumerable<ICppLibrary> GetCppLibs(Architecture arch)
	{
		yield return new XCodeCppLibrary(this);
	}

	public override NPath GetCompiler()
	{
		return FindClang();
	}

	public override NPath GetLinker()
	{
		return FindClang();
	}

	public override NPath GetArchiver()
	{
		return FindArTool();
	}

	private NPath FindClang()
	{
		if (XCodeLocation.DirectoryExists())
		{
			return XCodeClangLocation;
		}
		else if (DefaultClangLocation.Exists())
		{
			return DefaultClangLocation;
		}
		else
		{
			throw new Exception($"Clang not found: check path: {XCodeClangLocation} & {DefaultClangLocation}");
		}
	}

	private NPath FindArTool()
	{
		if (DefaultArToolLocation.Exists())
		{
			return DefaultArToolLocation;
		}
		else
		{
			throw new Exception($"Ar not found: check path: {DefaultArToolLocation}");
		}
	}

	internal class XCodeCppLibrary : ICppLibrary
	{
		public XCodeCppLibrary(XCodeSDK owner)
		{
			Owner = owner;
		}
		
		public IEnumerable<NPath> IncludePaths()
		{
			if (!Owner.XCodeLocation.Exists())
			{
				throw new Exception("XCode location not found");
			}

			yield return "/usr/include".ToNPath();
			yield return Owner.XCodeToolchainIncludeLocation;
			yield return Owner.PlatformSDK.SDKPath.Combine("usr/include");
			yield return Owner.PlatformSDK.SDKPath.Combine("usr/include/c++/v1");
		}

		public IEnumerable<NPath> LibraryPaths()
		{
			// yield return "/usr/lib".ToNPath();
			// yield return Owner.XCodeToolchainLibLocation;
			// yield return Owner.PlatformSDK.SDKPath.Combine("usr/lib");
			yield break;
		}

		public IEnumerable<string> StaticLibraries()
		{
			yield break;
		}

		public IEnumerable<string> DynamicLibraries()
		{
			yield return "c++";
		}

		private XCodeSDK Owner;
	}
}