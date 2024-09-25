using NiceIO;
using ReBuildTool.CppCompiler;
using ReBuildTool.Service.CompileService;
using ResetCore.Common;

namespace ReBuildTool.ToolChain.ThirdpartSupport.Unity;

public class UnityNativePluginArgs : CommandLineArgGroup<UnityNativePluginArgs>
{
	[CmdLine("unity native plugin root")]
	public CmdLineArg<string> UnityPluginRoot { get; set; }
}

public class UnityNativePluginSupport : BaseCppCompilePlugin
{
	public override void PreCompile(CppBuilder builder)
	{
		
	}

	public override void PostCompile(CppBuilder builder)
	{
		var unityArgs = UnityNativePluginArgs.Get();
		if (!string.IsNullOrEmpty(unityArgs.UnityPluginRoot))
		{
			CopyToUnity(builder.CurrentSource.OutputRoot
				, unityArgs.UnityPluginRoot.Value.ToNPath()
				, IPlatformSupport.CurrentTargetPlatform
				, builder.CurrentBuildOption.Configuration
				, builder.CurrentBuildOption.Architecture);
		}
	}

    
    private bool CopyToUnity(NPath binaryPath, NPath unityPluginRoot, PlatformSupportType platform, BuildConfiguration config, Architecture arch)
	{
		var fromPath = binaryPath
			.Combine(platform.ToString())
			.Combine(config.ToString())
			.Combine(arch.Name);
		var toPath = unityPluginRoot
			.Combine("Plugins");
		switch (platform)
		{
			case PlatformSupportType.Windows:
				toPath = toPath.Combine("Windows");
				if (arch is x86Architecture)
				{
					toPath = toPath.Combine("x86");
				}
				else if (arch is x64Architecture)
				{
					toPath = toPath.Combine("x86_64");
				}
				else
				{
					throw new Exception($"Windows not support arch {arch}");
				}
				break;
			case PlatformSupportType.Android:
				toPath = toPath.Combine("Android");
				if (arch is x86Architecture)
				{
					toPath = toPath.Combine("x86");
				}
				else if (arch is ARMv7Architecture)
				{
					toPath = toPath.Combine("armeabi-v7a");
				}
				else if (arch is ARM64Architecture)
				{
					toPath = toPath.Combine("arm64-v8a");
				}
				else
				{
					throw new Exception($"Android not support arch {arch}");
				}
				break;
			case PlatformSupportType.Linux:
				toPath = toPath.Combine("Linux");
				if (arch is x64Architecture)
				{
					toPath = toPath.Combine("x86_64");
				}
				else
				{
					throw new Exception("Linux not support arch {arch}");
				}
				break;
			case PlatformSupportType.MacOSX:
				toPath = toPath.Combine("Mac");
				break;
			case PlatformSupportType.iOS:
				toPath = toPath.Combine("iOS");
				toPath = toPath.Combine("iphoneos");
				break;
		}

		switch (config)
		{
			case BuildConfiguration.Debug:
				toPath = toPath.Combine("Debug");
				break;
			case BuildConfiguration.Release:
				toPath = toPath.Combine("Release");
				break;
			case BuildConfiguration.ReleasePlus:
				toPath = toPath.Combine("Release");
				break;
			case BuildConfiguration.ReleaseSize:
				toPath = toPath.Combine("Release");
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(config), config, null);
		}

		toPath.EnsureDirectoryExists();

		List<NPath> copyFiles = new();
		switch (platform)
		{
			case PlatformSupportType.Android:
			{
				copyFiles.AddRange(fromPath.Files()
					.Where(f => f.ExtensionWithDot == ".so" || f.ExtensionWithDot == ".a"));
			}
			break;
			case PlatformSupportType.Linux:
			{
				copyFiles.AddRange(fromPath.Files()
					.Where(f => f.ExtensionWithDot == ".so" || f.ExtensionWithDot == ".a"));
			}
			break;
			case PlatformSupportType.MacOSX:
			{
				copyFiles.AddRange(fromPath.Files()
					.Where(f => f.ExtensionWithDot == ".dylib" || f.ExtensionWithDot == ".a"));
			}
			break;
			case PlatformSupportType.Windows:
			{
				copyFiles.AddRange(fromPath.Files()
					.Where(f => f.ExtensionWithDot == ".dll" || f.ExtensionWithDot == ".lib" || f.ExtensionWithDot == ".pdb"));
			}
			break;
			case PlatformSupportType.iOS:
			{
				copyFiles.AddRange(fromPath.Files()
					.Where(f => f.ExtensionWithDot == ".a"));
			}
			break;
		}
		foreach (var file in copyFiles)
		{
			var toFile = toPath.Combine(file.FileName);
			Log.Info($"copy {file} to {toFile}");
			file.Copy(toFile);
		}
		return true;
	}
}