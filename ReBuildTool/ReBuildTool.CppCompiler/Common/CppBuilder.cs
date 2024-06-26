using ResetCore.Common;

namespace ReBuildTool.ToolChain;

public interface IBuildConfigProvider
{
	IPlatformSupport GetBuildPlatform();

	BuildOptions GetBuildOptions();
	
}

internal class BuildConfigArgsProvider : IBuildConfigProvider
{
	public IPlatformSupport GetBuildPlatform()
	{
		// TODO: use current platform instead
		return new WindowsPlatformSupport();
	}

	public BuildOptions GetBuildOptions()
	{
		return new BuildOptions();
	}
}

public partial class CppBuilder
{

	public static IBuildConfigProvider DefaultBuildConfigProvider { get; } = new BuildConfigArgsProvider();
	
	public CppBuilder(IBuildConfigProvider? provider = null)
	{
		provider ??= DefaultBuildConfigProvider;
		
		CurrentPlatformSupport = provider.GetBuildPlatform();
		CurrentBuildOption = provider.GetBuildOptions();
		var configuration = CurrentBuildOption.Configuration;
		var arch = CurrentBuildOption.Architecture;
		CurrentToolChain = CurrentPlatformSupport.MakeCppToolChain(arch, configuration);
	}

	public CppBuilder SetSource(ICppSourceProvider sourceProvider)
	{
		CurrentSource = sourceProvider;
		return this;
	}
	
	public void BuildTarget(TargetRule targetRule)
	{
		PendingTargetRule(targetRule);
		BuildPendingModules();
	}

	private void PendingTargetRule(TargetRule targetRule)
	{
		foreach (var moduleRule in targetRule.UsedModules)
		{
			if (!CurrentSource.ModuleRules.TryGetValue(moduleRule, out var module))
			{
				Log.Warning("cannot find module rule: " + moduleRule);
				continue;
			}

			if (PendingModulesQueue.Contains(module))
			{
				continue;
			}

			PendingModule(module);
		}	
	}
	
	private void PendingModule(ModuleRule module)
	{
		foreach (var dep in module.Dependencies)
		{
			if(!CurrentSource.ModuleRules.TryGetValue(dep, out var depModule))
			{
				Log.Warning("cannot find module rule: " + dep);
				continue;
			}
			PendingModule(depModule);
		}
		
		if (PendingModulesQueue.Contains(module))
        {
        	return;
        }
		
		PendingModulesQueue.Enqueue(module);
	}

	private void BuildPendingModules()
	{
		while(PendingModulesQueue.Count > 0)
		{
			var module = PendingModulesQueue.Dequeue();
			Log.Info($"Build {module.TargetName} Begin...");
			BuildModule(module);
			Log.Info($"Build {module.TargetName} Done...");
		}
	}
	
	private bool BuildModule(ModuleRule module)
	{
		CompileProcess process = CompileProcess.Create(module, this);
		if (!process.Compile())
		{
			Log.Error($"compile {module} failed !!");
			return false;
		}

		if (module.BuildType != BuildType.StaticLibrary)
		{
			if (!process.Link())
			{
				Log.Error($"link {module} failed !!");
				return false;
			}
		}
		else
		{
			if (!process.Archive())
			{
				Log.Error($"archive {module} failed !!");
				return false;
			}
		}
		
		return true;
	}
	
	private Queue<ModuleRule> PendingModulesQueue { get; } = new();

	public IToolChain CurrentToolChain { get; }
	
	public BuildOptions CurrentBuildOption { get; }
	
	public IPlatformSupport CurrentPlatformSupport { get; } 
	
	public ICppSourceProvider CurrentSource { get; private set; }
}