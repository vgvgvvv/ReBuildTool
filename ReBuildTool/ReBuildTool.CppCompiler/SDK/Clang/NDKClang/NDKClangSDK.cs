using NiceIO;
using ReBuildTool.Service.Global;

namespace ReBuildTool.ToolChain.SDK;

public class NDKClangSDK : ClangSDK
{
	
	public NDKClangSDK(NPath root, BuildEnvironmentPlatform buildPlatform) : base(root)
	{
		NDKVersion = root.FileName;
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
		return GetToolChainPath().Combine(execName);
	}

	public override NPath GetLinker()
	{
		var execName = "clang++";
		if (CurrentBuildPlatform == BuildEnvironmentPlatform.Windows)
		{
			execName += ".exe";
		}
		return GetToolChainPath().Combine(execName);
	}

	public override NPath GetArchiver()
	{
		var execName = "clang++";
		if (CurrentBuildPlatform == BuildEnvironmentPlatform.Windows)
		{
			execName += ".exe";
		}
		return GetToolChainPath().Combine(execName);
	}

	public NPath GetToolChainPath()
	{
		var root = RootPath.Combine("toolchains/llvm/prebuilt");
		string platformFolderName;
		switch (CurrentBuildPlatform)
		{
			case BuildEnvironmentPlatform.Windows:
				platformFolderName = "windows-x86_64";
				break;
			case BuildEnvironmentPlatform.Linux:
				platformFolderName = "linux-x86_64";
				break;
			case BuildEnvironmentPlatform.MacOSX:
				platformFolderName = "darwin-x86_64";
				break;
			default:
				throw new NotImplementedException($"not supported build platform {CurrentBuildPlatform}");
		}

		return root.Combine(platformFolderName).Combine("bin");
	}

	public string NDKVersion { get; }
	
	public BuildEnvironmentPlatform CurrentBuildPlatform { get; }
}

public class NDKClangCppLibrary : ICppLibrary
{
	public NDKClangSDK Owner { get; }
	public Architecture Arch { get; }

	public bool UseStaticCppLibrary { get; set; } = true;
	
	public NDKClangCppLibrary(NDKClangSDK owner, Architecture arch)
	{
		Owner = owner;
		Arch = arch;
	}
	
	public IEnumerable<NPath> IncludePaths()
	{
		var root = Owner.RootPath.Combine("sources/cxx-stl/");
		yield return root.Combine("llvm-libc++/include");
		yield return root.Combine("llvm-libc++abi/include");
		yield return root.Combine("system/include");
	}
	
	public IEnumerable<NPath> LibraryPaths()
	{
		yield return Owner.RootPath.Combine("sources/cxx-stl/llvm-libc++/libs").Combine(GetArchFolderName(Arch));
		yield return Owner.GetToolChainPath().Combine("lib");
		//yield return Owner.RootPath.Combine("sources/cxx-stl/llvm-libc++abi/libs").Combine(GetArchFolderName(Arch));
	}
	
	public IEnumerable<string> StaticLibraries()
	{
		if(UseStaticCppLibrary)
		{
			yield return "libc++_static.a";
			yield return "libc++abi.a";
		}
	}
	
	public IEnumerable<string> DynamicLibraries()
	{
		if (!UseStaticCppLibrary)
		{
			yield return "libc++_shared.so";
		}
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