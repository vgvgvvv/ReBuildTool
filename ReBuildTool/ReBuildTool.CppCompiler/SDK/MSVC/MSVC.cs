using System.Collections;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Setup.Configuration;
using NiceIO;

namespace ReBuildTool.ToolChain.SDK;

internal class MSVC
{
	// https://stackoverflow.com/questions/847048/finding-the-path-where-visual-studio-is-installed
	private const int REGDB_E_CLASSNOTREG = unchecked((int)0x80040154);
	internal static IEnumerable<(string, string)> GetVisualStudioInstallPaths() 
	{
		var result = new List<(string, string)>();

		try
		{
			var query = new SetupConfiguration() as ISetupConfiguration2;
			var e = query.EnumAllInstances();

			int fetched;
			var instances = new ISetupInstance[1];

			do
			{
				e.Next(1, instances, out fetched);

				if (fetched > 0)
				{
					var instance2 = (ISetupInstance2)instances[0];
					result.Add((instance2.GetInstallationVersion(), instance2.GetInstallationPath()));
				}
			}
			while (fetched > 0);
		}
		catch (COMException ex) when (ex.HResult == REGDB_E_CLASSNOTREG)
		{
			throw;
		}
		catch (Exception)
		{
			throw;
		}

		return result;
	}
	
	public static string GetArchFolderName(Architecture arch)
	{
		if (arch is x86Architecture)
		{
			return "x86";
		}
		else if(arch is x64Architecture)
		{
			return "x64";
		}
		else if(arch is ARMv7Architecture)
		{
			return "arm";
		}
		else if(arch is ARM64Architecture)
		{
			return "arm64";
		}
		else
		{
			throw new NotSupportedException($"not supported architecture {arch.GetType().Name}");
		}
	}
}

internal class VCPaths
{
	public VCPaths(NPath root)
	{
		VCRoot = root;
	}
	
	public NPath GetBinPath(Architecture arch)
	{
		return VCRoot.Combine("bin").Combine("Hostx64").Combine(MSVC.GetArchFolderName(arch));
	}
	
	public NPath GetIncludePath(Architecture arch)
	{
		return VCRoot.Combine("include");
	}
	
	public NPath GetLibPath(Architecture arch)
	{
		return VCRoot.Combine("lib").Combine(MSVC.GetArchFolderName(arch));
	}
	
	public NPath VCRoot { get; }
}

internal abstract class MsvcSDK : ICppLibrary
{
	public static Dictionary<Version, MsvcSDK> AllInstalledSDKs { get; } = new();

	static MsvcSDK()
	{
		if (AllInstalledSDKs.Count == 0)
		{
			foreach (var (versionStr, path) in MSVC.GetVisualStudioInstallPaths())
			{
				var version = Version.Parse(versionStr);
				var sdk = Create(version, path.ToNPath());
				if (sdk != null)
				{
					AllInstalledSDKs.Add(version, sdk);
				}
			}
		}
	}

	public static MsvcSDK FindLatestSDK()
	{
		var latest = AllInstalledSDKs.Keys.Max();
		return AllInstalledSDKs[latest];
	}
	
	protected static MsvcSDK? Create(Version version, NPath installPath)
	{
		// if(version.Major == 15)
		// {
		// 	return Msvc15.Create(version, installPath);
		// }
		// else 
		if (version.Major == 17)
		{
			return Msvc17.Create(version, installPath);
		}
		else
		{
			return null;
		}
	}
	
	protected MsvcSDK(Version version, NPath installPath)
	{
		Version = version;
		InstallPath = installPath;
	}

	public MsvcSDK SetWindowsKit(WindowsKit kit)
	{
		CurrentWindowsKit = kit;
		return this;
	}
	
	public MsvcSDK UseLatestWindowsKit()
	{
		var latestVersion = WindowsKit.AllInstalledKits.Keys.Max();
		CurrentWindowsKit = WindowsKit.AllInstalledKits[latestVersion];
		return this;
	}

	public MsvcSDK SetVCPaths(VCPaths vc)
	{
		CurrentVCPaths = vc;
		return this;
	}
	
	public MsvcSDK UseLastestVCPaths()
	{
		var vcPaths = GetAllVCPaths();
		var latestVerions = vcPaths.Keys.Max();
		CurrentVCPaths = vcPaths[latestVerions];
		return this;
	}

	public MsvcSDK SetArch(Architecture arch)
	{
		CurrentArchitecture = arch;
		return this;
	}

	public bool IsReadyToUse()
	{
		return CurrentArchitecture != null && CurrentVCPaths != null && CurrentWindowsKit != null;
	}
	
	public virtual IEnumerable<NPath> GetIncludeDirectories()
	{
		return GetSdkIncludeDirectories().Concat(GetVcIncludeDirectories());
	}

	public virtual IEnumerable<NPath> GetLibraryDirectories()
	{
		return GetSdkLibraryDirectories().Concat(GetVcLibraryDirectories());
	}

	public abstract IEnumerable<NPath> GetSdkIncludeDirectories();
	public abstract IEnumerable<NPath> GetSdkLibraryDirectories();
	public abstract IEnumerable<NPath> GetVcIncludeDirectories();
	public abstract IEnumerable<NPath> GetVcLibraryDirectories();

	public abstract NPath GetVcToolRootPath();

	public Dictionary<Version, VCPaths> GetAllVCPaths()
	{
		var result = new Dictionary<Version, VCPaths>();
		foreach (var directory in GetVcToolRootPath().Directories())
		{
			var version = Version.Parse(directory.FileName);
			result.Add(version, new VCPaths(directory));
		}
		return result;
	}
	
	public abstract NPath CompilerPath { get; }
	
	public abstract NPath LinkerPath { get; }
	
	public abstract NPath ArchiverPath { get; }
	
	public abstract NPath AsmCompilerPath { get; }

	public abstract IEnumerable<string> PathEnvironmentVariable { get; }
	
	public Architecture? CurrentArchitecture { get; private set; }
	public WindowsKit? CurrentWindowsKit { get; private set; }
	public VCPaths? CurrentVCPaths { get; private set; }
	public Version Version { get; }
	public NPath InstallPath { get; }
	
	public IEnumerable<NPath> IncludePaths()
	{
		foreach (var sdkInclude in GetSdkIncludeDirectories())
		{
			yield return sdkInclude;
		}

		foreach (var vcInclude in GetVcIncludeDirectories())
		{
			yield return vcInclude;
		}
	}

	public IEnumerable<NPath> LibraryPaths()
	{
		foreach (var sdkLibrary in GetSdkLibraryDirectories())
		{
			yield return sdkLibrary;
		}

		foreach (var vcLibrary in GetVcLibraryDirectories())
		{
			yield return vcLibrary;
		}
	}

	public IEnumerable<string> StaticLibraries()
	{
		yield break;
	}

	public IEnumerable<string> DynamicLibraries()
	{
		yield break;
	}
}