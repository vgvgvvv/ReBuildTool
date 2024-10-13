using NiceIO;
using ReBuildTool.Actions;
using ReBuildTool.CppCompiler;
using ReBuildTool.Service.Global;
using ReBuildTool.ToolChain;
using ResetCore.Common;

namespace ReBuildTool.Service.CompileService.HeaderTool;

public class HeaderToolArgs : CommandLineArgGroup<HeaderToolArgs>
{
	[CmdLine("header tool root")]
	public CmdLineArg<string> HeaderToolRoot { get; set; }
	
	[CmdLine("need build header tool")]
	public CmdLineArg<bool> NeedBuildHeaderTool { get; set; } = CmdLineArg<bool>.FromObject(nameof(NeedBuildHeaderTool), false);
}

public class HeaderToolPluginSupport : BaseCppTargetCompilePlugin
{
	private NPath _headerTooRoot;
	public NPath HeaderToolRoot
	{
		get
		{
			if (_headerTooRoot == null || !_headerTooRoot.Exists())
			{
				if (CodeSource == null)
				{
					throw new Exception("CodeSource is null, please setup first !!");
				}
				var headerToolArgs = HeaderToolArgs.Get();
				if (headerToolArgs.HeaderToolRoot.IsSet)
				{
					_headerTooRoot = headerToolArgs.HeaderToolRoot.Value.ToNPath();
				}
				else
				{
					_headerTooRoot = CodeSource.IntermediaFolder.Combine("ResetHeaderTool");
				}
			}
			
			return _headerTooRoot;
		}
	}

	public NPath HeaderToolInstallPath => HeaderToolRoot.Combine("ResetHeaderTool");

	public NPath HeaderToolExePath
	{
		get
		{
			var platformFolderName = PlatformHelper.Pick("Win64", "MacArm64", "Linux");
			var ex = PlatformHelper.IsWindows() ? ".exe" : "";
			return HeaderToolInstallPath.Combine("Binary").Combine(platformFolderName).Combine($"ResetHeaderTool{ex}");
		}
	}
	
	public override void Setup(ICppSourceProviderInterface sourceProvider)
	{
		CodeSource = sourceProvider;
		BuildHeaderTool();
	}

	public override void PreCompile(CppTargetRule targetRule, CppBuilder builder)
	{
		base.PreCompile(targetRule, builder);
		var shell = Shell.Create()
			.WithProgram(HeaderToolExePath)
			.WithArguments(GetCmdArgs(targetRule, builder))
			.Execute()
			.WaitForEnd();

		if (shell.Process.ExitCode != 0)
		{
			throw new Exception("run header tool failed");
		}
		
	}
	
	public override void PostCompile(CppTargetRule targetRule, CppBuilder builder)
	{
		base.PostCompile(targetRule, builder);
	}

	public IEnumerable<string> GetCmdArgs(CppTargetRule targetRule, CppBuilder builder)
	{
		var projectArgs = CppCompilerArgs.Get();

		yield return $"projectPath={projectArgs.ProjectRoot}";
		if (builder.CurrentBuildOption.Configuration == BuildConfiguration.Debug)
		{
			yield return "debug=true";
		}

		if (builder.CurrentPlatformSupport is WindowsPlatformSupport)
		{
			yield return "targetplatform=Win64";
		}
		else if(builder.CurrentPlatformSupport is MacOSXPlatformSupport)
		{
			yield return "targetplatform=Mac";
		}
		else if(builder.CurrentPlatformSupport is LinuxPlatformSupport)
		{
			yield return "targetplatform=Linux";
		}
		else if(builder.CurrentPlatformSupport is iOSPlatformSupport)
		{
			yield return "targetplatform=IOS";
		}
		else if (builder.CurrentPlatformSupport is AndroidPlatformSupport)
		{
			yield return "targetplatform=Android";
		}
		else
		{
			throw new Exception("not support platform");
		}
		
		// pluginDlls=
		// plugins
		yield return "projectType=Custom";
		// customProject
	}

	private void BuildHeaderTool()
	{
		bool needBuild = false;
		var headerToolArgs = HeaderToolArgs.Get();
		var installPath = HeaderToolRoot.Combine("ResetHeaderTool");
		if (installPath.Combine(".git").Exists())
		{
			if (headerToolArgs.NeedBuildHeaderTool)
			{
				Git.Pull(installPath);
				needBuild = true;
			}
		}
		else
		{
			HeaderToolRoot.EnsureDirectoryExists();
			Git.GetFromGit("git@github.com:vgvgvvv/ResetHeaderTool.git", "ResetHeaderTool", HeaderToolRoot);
			needBuild = true;
		}

		if (!needBuild)
		{
			return;
		}

		var buildScript = installPath.Combine("Scripts/BuildAll.bat");
		if (!PlatformHelper.IsWindows())
		{
			buildScript.ChangeExtension(".sh");
		}
		Cmd.RunCmd(buildScript, "", HeaderToolRoot);
	}

	private ICppSourceProviderInterface CodeSource;
}