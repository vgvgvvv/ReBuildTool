using System.Reflection;
using Bullseye;
using NiceIO;
using ReBuildTool.Common;
using ReBuildTool.CppCompiler.Standalone;
using ReBuildTool.Internal.Ini;
using ReBuildTool.Internal.Lua;
using ReBuildTool.Service.CompileService;
using ReBuildTool.Service.Context;
using ReBuildTool.ToolChain;
using ReBuildTool.ToolChain.Project;
using ResetCore.Common;

namespace ReBuildTool.Internal;

public class BuildActionMeta
{
	public MethodInfo? Method { get; set; }
	public ActionDefineAttribute? Attribute { get; set; }
}

public interface IBuildItem : ITargetItem
{
	public void SetupInitTargets(Targets targets, ref List<string> newTargets);
	public void SetupBuildTargets(Targets targets, ref List<string> newTargets);
}

public interface ITargetItem
{
	public string Name { get; }
}

public abstract class ModuleBase : ITargetItem
{
	
	public abstract string ModuleFileExtension { get; }
	public abstract string TargetFileExtension { get; }
	
	public ModuleBase(string modulePath, ModuleProject owner)
	{
		ModulePath = modulePath;
		Owner = owner;
		var fileName = Path.GetFileName(ModulePath);
		Name = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(fileName));
	}

	public string ModulePath { get; }
	public string Name { get; }
	public ModuleProject Owner { get; }

}

internal class TargetScope : IDisposable
{
	public TargetScope(ITargetItem item)
	{
		CurrentItem = new TargetScopeItem(item);
		ScopeStack.Push(CurrentItem);
	}
	
	public TargetScope AddDependency(string? dependency)
	{
		if (dependency != null)
		{
			CurrentItem.DependOn.Add(dependency);
		}

		return this;
	}
	
	public TargetScope AddDependencies(List<string>? dependencies)
	{
		if (dependencies != null)
		{
			foreach (var dependency in dependencies)
			{
				AddDependency(dependency);
			}
		}

		return this;
	}
	
	public TargetScope SetArg(string key, object value)
	{
		CurrentItem.Args[key] = value;
		return this;
	}

	public void Dispose()
	{
		ScopeStack.Pop();
	}

	public static string GetCurrentItemName()
	{
		var items = ScopeStack.ToList();
		items.Reverse();
		return string.Join(".", items.Select(item => item.Item.Name));
	}
	
	public static List<string> GetCurrentItemDependOn()
	{
		return ScopeStack.SelectMany(item => item.DependOn).ToList();
	}

	public static bool GetArg<T>(string argName, out T? arg)
	{
		var scopeList = ScopeStack.ToList();
		scopeList.Reverse();
		foreach (var item in scopeList)
		{
			if (!item.Args.TryGetValue(argName, out object? outArg))
			{
				continue;
			}

			arg = (T?)outArg;
			if (arg == null)
			{
				continue;
			}

			return true;
		}

		arg = CmdParser.GetArg<T>(argName);
		return arg != null;
	}

	public class TargetScopeItem
	{
		public ITargetItem Item;
		public List<string> DependOn;
		public Dictionary<string, object> Args;

		public TargetScopeItem(ITargetItem item, List<string>? dependOn = null)
		{
			Item = item;
			DependOn = dependOn ?? new List<string>();
			Args = new();
		}
	}

	public TargetScopeItem CurrentItem { get; }
	public static Stack<TargetScopeItem> ScopeStack { get; } = new();
}

public class ModuleProject : IBuildItem
{
	public static ModuleProject Current { get; private set; }
	
	public ICppProject CppProject { get; private set; }

	private ModuleProject(string target)
	{
		TargetName = target;
		Current = this;
	}

	public static ModuleProject Create(string target)
	{
		var targetFile = GlobalPaths.ProjectRoot.Combine($"{target}{IniModuleBase.StaticTargetFileExtension}");
		if (!targetFile.Exists())
		{
			var defaultContent = @"
[Target]

[Init]
+Actions=(Name=""ReMake.Init"", Args=(projectName=""${targetName}""))
";

			ContextArgs.Context context = new ContextArgs.Context();
			context.AddArg("targetName", target);
			ContextArgs args = new ContextArgs(defaultContent);
			targetFile.WriteAllText(args.GetText(context));
		}
		return new ModuleProject(target);
	}

	public ModuleProject Parse(string path)
	{
		CppProject = 
			ServiceContext.Instance.Create<ICppProject>(CppCompilerArgs.Get().CppBuildRoot.ToNPath()).Value;
		CppProject.Parse();
		ParseInternal(path);
		return this;
	}

	public string Name => "ProjectRoot";
	public string GetTargetName()
	{
		return Name;
	}

	public void SetupInitTargets(Targets targets, ref List<string> newTargets)
	{
		targets.Add("CppProjectInit", ()=>CppProject.Setup());
		if (IniTargetsToHandle.TryGetValue(TargetName, out var targetToHandle))
		{
			var targetTargets = new List<string>();
			targetToHandle.SetupInitTargets(targets, ref targetTargets);
			newTargets.AddRange(targetTargets);
		}
		else
		{
			Log.Exception($"cannot find target {TargetName}");
		}
	}

	public void SetupBuildTargets(Targets targets, ref List<string> newTargets)
	{
		targets.Add("CppProjectBuild", ()=>CppProject.Build());
		if (IniTargetsToHandle.TryGetValue(TargetName, out var targetToHandle))
		{
			using (new TargetScope(this))
			{
				var targetTargets = new List<string>();
				targetToHandle.SetupBuildTargets(targets, ref targetTargets);
				newTargets.AddRange(targetTargets);
			}
		}
		else
		{
			Log.Exception($"cannot find target {TargetName}");
		}
	}


	private void ParseInternal(string path)
	{
		{
			var moduleFiles = Directory.GetFiles(path, $"*{IniModuleBase.StaticModuleFileExtension}",
				SearchOption.TopDirectoryOnly);
			var modules = moduleFiles.Select(file => new IniModule(file, this));
			foreach (var module in modules)
			{
				IniModulesToHandle.Add(module.Name, module);
			}
		}

		{
			var targetFiles = Directory.GetFiles(path, $"*{IniModuleBase.StaticTargetFileExtension}",
				SearchOption.TopDirectoryOnly);
			var targets = targetFiles.Select(file => new IniTarget(file, this));
			foreach (var target in targets)
			{
				IniTargetsToHandle.Add(target.Name, target);
			}
		}
		
		{
			var moduleFiles = Directory.GetFiles(path, $"*{LuaModuleBase.StaticModuleFileExtension}",
				SearchOption.TopDirectoryOnly);
			var modules = moduleFiles.Select(file => new LuaModule(file, this));
			foreach (var module in modules)
			{
				LuaModulesToHandle.Add(module.Name, module);
			}
		}

		foreach (var directory in Directory.GetDirectories(path))
		{
			ParseInternal(directory);
		}
	}

	public IniModule? GetModule(string name)
	{
		if (!IniModulesToHandle.TryGetValue(name, out var module))
		{
			Log.Exception($"cannot find module {name}");
			return null;
		}

		return module;
	}

	public string TargetName { get; }

	private Dictionary<string, IniModule> IniModulesToHandle { get; } = new();
	private Dictionary<string, IniTarget> IniTargetsToHandle { get; } = new();
	private Dictionary<string, LuaModule> LuaModulesToHandle { get; } = new();
}