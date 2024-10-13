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
	
	public override void Setup(ICppSourceProviderInterface sourceProvider)
	{
		CodeSource = sourceProvider;
		BuildHeaderTool();
	}

	public override void PreCompile(CppTargetRule targetRule, CppBuilder builder)
	{
		base.PreCompile(targetRule, builder);
	}
	
	public override void PostCompile(CppTargetRule targetRule, CppBuilder builder)
	{
		base.PostCompile(targetRule, builder);
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