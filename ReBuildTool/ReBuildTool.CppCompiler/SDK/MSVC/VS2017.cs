using NiceIO;

namespace ReBuildTool.ToolChain.SDK;

internal class Msvc15 : MsvcSDK
{
	public static MsvcSDK Create(Version version, NPath path)
	{
		return new Msvc15(version, path);
	}

	protected Msvc15(Version version, NPath path) : base(version, path)
	{
	}
	
	public override IEnumerable<NPath> GetSdkIncludeDirectories()
	{
		foreach (var includeDirectory in CurrentWindowsKit.GetIncludeDirectories())
		{
			foreach (var dir in includeDirectory.Directories())
			{
				yield return dir;
			}
		}
	}

	public override IEnumerable<NPath> GetSdkLibraryDirectories()
	{
		foreach (var libraryDirectory in CurrentWindowsKit.GetLibraryDirectories())
		{
			foreach (var directory in libraryDirectory.Directories())
			{
				yield return directory.Combine(MSVC.GetArchFolderName(CurrentArchitecture));
			}
		}
	}

	public override IEnumerable<NPath> GetVcIncludeDirectories()
	{
		yield return CurrentVCPaths.GetIncludePath(CurrentArchitecture);
	}

	public override IEnumerable<NPath> GetVcLibraryDirectories()
	{
		yield return CurrentVCPaths.GetLibPath(CurrentArchitecture);
	}

	public override NPath GetVcToolRootPath()
	{
		return InstallPath.Combine("VC/Tools/MSVC");
	}

	public override NPath CompilerPath => CurrentVCPaths.GetBinPath(CurrentArchitecture).Combine("cl.exe");
	public override NPath LinkerPath => CurrentVCPaths.GetBinPath(CurrentArchitecture).Combine("link.exe");
	public override NPath ArchiverPath => CurrentVCPaths.GetBinPath(CurrentArchitecture).Combine("lib.exe");

	public override NPath AsmCompilerPath
	{
		get
		{
			if(CurrentArchitecture is x86Architecture)
			{
				return CurrentVCPaths.GetBinPath(CurrentArchitecture).Combine("ml.exe");
			}
			else
			{
				return CurrentVCPaths.GetBinPath(CurrentArchitecture).Combine("ml64.exe");
			}
		}
	}

	public override IEnumerable<string> PathEnvironmentVariable
	{
		get
		{
			yield return CurrentVCPaths.GetBinPath(CurrentArchitecture);
		}
	}
	

}