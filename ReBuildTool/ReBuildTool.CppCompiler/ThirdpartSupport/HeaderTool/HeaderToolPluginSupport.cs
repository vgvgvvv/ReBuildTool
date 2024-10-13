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

public interface IHeaderToolTarget
{
	List<string> PluginDlls { get; }
	
	List<string> PluginNames { get; }
	
	List<string> ExtraArgs { get; }
}

public partial class HeaderToolPluginSupport : BaseCppTargetCompilePlugin
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
			return HeaderToolInstallPath.Combine("Binary").Combine(platformFolderName).Combine($"HeaderTool/ResetHeaderTool{ex}");
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
		var headerToolTarget = targetRule as IHeaderToolTarget;
		if (headerToolTarget == null)
		{
			throw new Exception("targetRule is not IHeaderToolTarget");
		}

		CodeSource = builder.CurrentSource;
		GenerateProjectInfoForHeaderTool(targetRule, builder);
		RunHeaderTool(targetRule, builder);
	}
	
	public override void PostCompile(CppTargetRule targetRule, CppBuilder builder)
	{
		base.PostCompile(targetRule, builder);
	}

	private ICppSourceProviderInterface CodeSource;
}