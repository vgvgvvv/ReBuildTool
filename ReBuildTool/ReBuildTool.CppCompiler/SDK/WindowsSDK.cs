using NiceIO;

namespace ReBuildTool.ToolChain.SDK;

// only support for win10 now
internal class WindowsKit
{
	public static Dictionary<Version, WindowsKit> AllInstalledKits { get; } = new();

	static WindowsKit()
	{
		foreach (var directory in Win10KitRoot.Combine("Include").Directories())
		{
			var version = Version.Parse(directory.FileName);
			AllInstalledKits.Add(version, Create(version));
		}
	}
	
	public static NPath WindowsKitRoot
	{
		get
		{
			var root = "C:/Program Files (x86)/Windows Kits".ToNPath();
			if (!root.Exists())
			{
				throw new DirectoryNotFoundException("cannot find Windows Kits directory");
			}
			return root;
		}
	}
	
	public static NPath Win10KitRoot
	{
		get
		{
			var root = WindowsKitRoot.Combine("10");
			if (!root.Exists())
			{
				throw new DirectoryNotFoundException("cannot find Windows 10 Kits directory");
			}
			return root;
		}
	}
	
	public static NPath Win8_1KitRoot
	{
		get
		{
			var root = WindowsKitRoot.Combine("8.1");
			if (!root.Exists())
			{
				throw new DirectoryNotFoundException("cannot find Windows 8.1 Kits directory");
			}
			return root;
		}
	}

	public static WindowsKit Create(Version version)
	{
		if (version.Major == 10)
		{
			return new WindowsKit(version, Win10KitRoot);
		}
		else
		{
			throw new NotSupportedException("Windows Kit version not supported");
		}
	}

	private WindowsKit(Version version, NPath root)
	{
		Version = version;
		RootPath = root;
	}
	
	public IEnumerable<NPath> GetIncludeDirectories()
	{
		var includePath = RootPath.Combine("Include", Version.ToString());
		if (!includePath.Exists())
		{
			throw new DirectoryNotFoundException("cannot find Windows Kit include directory");
		}
		yield return includePath;
	}
	
	public IEnumerable<NPath> GetLibraryDirectories()
	{
		var libPath = RootPath.Combine("Lib", Version.ToString());
		if (!libPath.Exists())
		{
			throw new DirectoryNotFoundException("cannot find Windows Kit library directory");
		}
		yield return libPath;
	}
	
	public IEnumerable<NPath> GetBinDirectories()
	{
		var binPath = RootPath.Combine("Bin", Version.ToString());
		if (!binPath.Exists())
		{
			throw new DirectoryNotFoundException("cannot find Windows Kit bin directory");
		}
		yield return binPath;
	}
	
	public IEnumerable<NPath> GetSourceDirectories()
	{
		var sourcePath = RootPath.Combine("Source", Version.ToString());
		if (!sourcePath.Exists())
		{
			throw new DirectoryNotFoundException("cannot find Windows Kit bin directory");
		}
		yield return sourcePath;
	}

	
	
	public Version Version { get; }
	public NPath RootPath { get; }
}