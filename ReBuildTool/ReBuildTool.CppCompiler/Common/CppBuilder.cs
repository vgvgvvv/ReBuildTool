using NiceIO;
using ReBuildTool.CppCompiler;
using ReBuildTool.Service.CompileService;
using ReBuildTool.Service.Global;
using ReBuildTool.ToolChain.Project;
using ReBuildTool.ToolChain.SDK.MakeFile;

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
		return IPlatformSupport.CurrentTargetPlatformSupport;
	}

	public BuildOptions GetBuildOptions()
	{
		return BuildOptions.CreateDefault(GetBuildPlatform());
	}
}

public interface ICppBuildContext
{
	public IToolChain CurrentToolChain { get; }
	
	public BuildOptions CurrentBuildOption { get; }
	
	public IPlatformSupport CurrentPlatformSupport { get; } 
	
	public ICppSourceProviderInterface CurrentSource { get; }
	
	public NPath OutputRoot { get; }
}

public partial class CppBuilder : ICppBuildContext
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

	public CppBuilder SetSource(ICppSourceProviderInterface sourceProvider)
	{
		CurrentSource = sourceProvider;
		return this;
	}
	
	public void BuildTarget(ITargetInterface targetRule)
	{
		PendingTargetRule(targetRule);
		BuildPendingModules();
	}

	private void PendingTargetRule(ITargetInterface targetRule)
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
	
	private void PendingModule(IModuleInterface module)
	{
		if (module is CppModuleRule cppModuleRule)
		{
			cppModuleRule.SetupInternal(this);
		}
		if (!module.IsSupport)
		{
			return;
		}
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
		bool succ = true;
		bool useMakeFile = CppCompilerArgs.Get().UseMakeFileBuild.Value;
		while(PendingModulesQueue.Count > 0)
		{
			var module = PendingModulesQueue.Dequeue();
			Log.Info($"Build {module.TargetName} Begin...");
			if (useMakeFile)
			{
				BuildMakeFile(module, out var makeFilePath);
				if (!MakeFile.RunMakeFile(makeFilePath))
				{
					succ = false;
				}
			}
			else
			{
				if (!BuildModule(module))
				{
					succ = false;
				}
			}
			
			Log.Info($"Build {module.TargetName} Done...");
		}

		if (succ == false)
		{
			throw new Exception("build modules failed !!");
		}
	}
	
	private bool BuildModule(IModuleInterface module)
	{
		CompileProcess process = CompileProcess.Create(module, this);
		if (!process.Compile())
		{
			Log.Error($"compile {module} failed !!");
			return false;
		}

		if (module.TargetBuildType != BuildType.StaticLibrary)
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

		if (module is IPostBuildModule postBuildModule)
		{
			postBuildModule.PostBuild();
		}
		
		return true;
	}

	private bool BuildMakeFile(IModuleInterface module, out NPath makeFilePath)
	{
		CompileProcess process = CompileProcess.Create(module, this);
		makeFilePath = process.GenerateMakeFile();
		return !string.IsNullOrEmpty(File.ReadAllText(makeFilePath));
	}
	
	private Queue<IModuleInterface> PendingModulesQueue { get; } = new();

	public IToolChain CurrentToolChain { get; }
	
	public BuildOptions CurrentBuildOption { get; }
	
	public IPlatformSupport CurrentPlatformSupport { get; } 
	
	public ICppSourceProviderInterface CurrentSource { get; private set; }
	
	public NPath OutputRoot => CurrentSource.OutputRoot
		.Combine(IPlatformSupport.CurrentTargetPlatform.ToString())
		.Combine(CurrentBuildOption.Configuration.ToString())
		.Combine(CurrentBuildOption.Architecture.Name);
}