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

		// Wire each module's RHT-generated output into the compile flow now that
		// the tool has run (so the dirs exist) but before BuildTarget collects
		// compile units. Doing it here — after ModuleInfo serialization — keeps
		// these paths out of the HeaderTool input, which would otherwise crash on
		// the not-yet-existing HeaderToolGen dir. See InjectHeaderToolGenPaths.
		InjectHeaderToolGenPaths(builder);
	}

	/// <summary>
	/// After the HeaderTool has run, connect each module's generated
	/// <c>HeaderToolGen</c> tree to the compile pipeline:
	/// <list type="bullet">
	/// <item><c>&lt;ModuleDir&gt;/HeaderToolGen</c> → <see cref="CppModuleRule.SourceDirectories"/>,
	///   so the generated <c>.ext.gen.cpp</c> files are compiled.</item>
	/// <item><c>&lt;ModuleDir&gt;/HeaderToolGen/Extension</c> → <see cref="CppModuleRule.PublicIncludePaths"/>,
	///   so sources can <c>#include "Xxx.extension.h"</c> by bare name, and so
	///   modules depending on this one see those headers transitively.</item>
	/// </list>
	/// Only injects when the directory actually exists — modules without
	/// reflection annotations produce no <c>HeaderToolGen</c> output. Uses
	/// <c>Contains</c> to avoid accumulating duplicates across repeated setups.
	/// </summary>
	private void InjectHeaderToolGenPaths(CppBuilder builder)
	{
		foreach (var (_, module) in builder.CurrentSource.ModuleRules)
		{
			if (module is not CppModuleRule rule)
			{
				continue;
			}
			if (string.IsNullOrEmpty(rule.ModuleDirectory))
			{
				continue;
			}

			var headerToolGen = rule.ModuleDirectory.ToNPath().Combine("HeaderToolGen");
			if (!headerToolGen.Exists())
			{
				continue;
			}

			var genPath = headerToolGen.ToString();
			if (!rule.SourceDirectories.Contains(genPath))
			{
				rule.SourceDirectories.Add(genPath);
			}

			var extension = headerToolGen.Combine("Extension");
			if (extension.Exists())
			{
				var extPath = extension.ToString();
				if (!rule.PublicIncludePaths.Contains(extPath))
				{
					rule.PublicIncludePaths.Add(extPath);
				}
			}
		}
	}
	
	public override void PostCompile(CppTargetRule targetRule, CppBuilder builder)
	{
		base.PostCompile(targetRule, builder);
	}

	private ICppSourceProviderInterface CodeSource;
}