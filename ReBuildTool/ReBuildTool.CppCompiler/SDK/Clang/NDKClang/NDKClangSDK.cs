using NiceIO;

using ReBuildTool.CppCompiler;
using ReBuildTool.Service.Global;

namespace ReBuildTool.ToolChain.SDK;

public abstract class NDKTargetArchSetting
{
	public abstract string Version { get; }
	public abstract string TargetPlatformName { get; }
	public string TargetName => $"{TargetPlatformName}{Version}";
}

public class ARM32NDKTargetArchSetting : NDKTargetArchSetting
{
	public override string Version => AndroidCompilerArgs.Get().NDKTargetVersion.Value.ToString();
	public override string TargetPlatformName => "arm-linux-androideabi";
}

public class ARM64NDKTargetArchSetting : NDKTargetArchSetting
{
	public override string Version => AndroidCompilerArgs.Get().NDKTargetVersion.Value.ToString();
	public override string TargetPlatformName => "aarch64-linux-android";
}

public class X86NDKTargetArchSetting : NDKTargetArchSetting
{
	public override string Version => AndroidCompilerArgs.Get().NDKTargetVersion.Value.ToString();
	public override string TargetPlatformName => "i686-linux-android";
}


public class X64NDKTargetArchSetting : NDKTargetArchSetting
{
	public override string Version => AndroidCompilerArgs.Get().NDKTargetVersion.Value.ToString();
	public override string TargetPlatformName => "x86_64-linux-android";
}

public class NDKClangSDK : ClangSDK
{
	public NDKClangSDK(NPath root, BuildEnvironmentPlatform buildPlatform, Architecture arch) : base(root)
	{
		NDKVersion = root.FileName;
		switch (arch)
		{
			case ARM64Architecture arm64Architecture:
				Setting = new ARM64NDKTargetArchSetting();
				break;
			case ARMv7Architecture arMv7Architecture:
				Setting = new ARM32NDKTargetArchSetting();
				break;
			case x64Architecture x64Architecture:
				Setting = new X64NDKTargetArchSetting();
				break;
			case x86Architecture x86Architecture:
				Setting = new X86NDKTargetArchSetting();
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(arch));
		}
		CurrentBuildPlatform = buildPlatform;
	}

	public override IEnumerable<ICppLibrary> GetCppLibs(Architecture arch)
	{
		yield return new NDKClangCppLibrary(this, arch);
	}

	public override NPath GetCompiler()
	{
		var execName = "clang++";
		if (CurrentBuildPlatform == BuildEnvironmentPlatform.Windows)
		{
			execName += ".exe";
		}
		return LLVMPath.Combine("bin").Combine(execName);
	}

	public override NPath GetLinker()
	{
		var execName = "clang++";
		if (CurrentBuildPlatform == BuildEnvironmentPlatform.Windows)
		{
			execName += ".exe";
		}
		return LLVMPath.Combine("bin").Combine(execName);
	}

	public override NPath GetArchiver()
	{
		var execName = "clang++";
		if (CurrentBuildPlatform == BuildEnvironmentPlatform.Windows)
		{
			execName += ".exe";
		}
		return LLVMPath.Combine("bin").Combine(execName);
	}

	public string NDKVersion { get; }
	public NPath SysRootIncludePath => SysRoot.Combine("usr/include");
	public NPath SysRootLibPath => SysRoot.Combine("usr/lib").Combine(Setting.TargetPlatformName);
	public NPath SysRootLibVersionedPath => SysRoot.Combine("usr/lib").Combine(Setting.TargetPlatformName).Combine(Setting.Version);
	public NPath LLVMPath => RootPath.Combine("toolchains/llvm/prebuilt").Combine(PlatformFolderName);
	
	public NPath SysRoot => LLVMPath.Combine("sysroot");
	public string PlatformFolderName => PlatformHelper.Pick("windows-x86_64", "linux-x86_64", "darwin-x86_64");
	public NDKTargetArchSetting Setting { get; }
	public BuildEnvironmentPlatform CurrentBuildPlatform { get; }
}

public class NDKClangCppLibrary : ICppLibrary
{
	public NDKClangSDK Owner { get; }
	public Architecture Arch { get; }

	public bool UseStaticCppLibrary { get; set; } = false;
	
	public NDKClangCppLibrary(NDKClangSDK owner, Architecture arch)
	{
		Owner = owner;
		Arch = arch;
	}
	
	public IEnumerable<NPath> IncludePaths()
	{
		yield return Owner.SysRootLibVersionedPath; // for crtbegin_so.o & crtend_so.o
		yield return Owner.SysRootIncludePath.Combine("c++/v1");
	}
	
	public IEnumerable<NPath> LibraryPaths()
	{
		// https://stackoverflow.com/questions/19768267/relocation-r-x86-64-32s-against-linking-error
		// yield return Owner.SysRootLibPath; // dont use this, it cause link failed...
		
		yield return Owner.SysRootLibVersionedPath;
		//yield return Owner.RootPath.Combine("sources/cxx-stl/llvm-libc++abi/libs").Combine(GetArchFolderName(Arch));
	}
	
	public IEnumerable<string> StaticLibraries()
	{
		if(UseStaticCppLibrary)
		{
			yield return "c++";
			yield return "c++abi";
		}
	}
	
	public IEnumerable<string> DynamicLibraries()
	{
		if (!UseStaticCppLibrary)
		{
			yield return "c++";
		}
		yield return "log";

	}

	private string GetArchFolderName(Architecture arch)
	{
		if (arch == new ARM64Architecture())
		{
			return "arm64-v8a";
		}
		else if (arch == new x86Architecture())
		{
			return "armeabi-v7a";
		}
		else if (arch == new x86Architecture())
		{
			return "x86";
		}
		else if(arch == new x64Architecture())
		{
			return "x86_64";
		}
		else
		{
			throw new NotImplementedException($"not supported architecture {arch.Name}");
		}
	}
		
}