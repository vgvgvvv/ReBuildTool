using System.Reflection;
using Bullseye;
using ReBuildTool.Common;
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

	private ModuleProject(string target)
	{
		TargetName = target;
		Current = this;
	}

	public static ModuleProject Create(string target)
	{
		return new ModuleProject(target);
	}

	public ModuleProject Parse(string path)
	{
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
		if (!TargetsToHandle.TryGetValue(TargetName, out var targetToHandle))
		{
			Log.Exception($"cannot find target {TargetName}");
			return;
		}

		var targetTargets = new List<string>();
		targetToHandle.SetupInitTargets(targets, ref targetTargets);
		newTargets.AddRange(targetTargets);
	}

	public void SetupBuildTargets(Targets targets, ref List<string> newTargets)
	{
		if (!TargetsToHandle.TryGetValue(TargetName, out var targetToHandle))
		{
			Log.Exception($"cannot find target {TargetName}");
			return;
		}

		using (new TargetScope(this))
		{
			var targetTargets = new List<string>();
			targetToHandle.SetupBuildTargets(targets, ref targetTargets);
			newTargets.AddRange(targetTargets);
		}
	}


	private void ParseInternal(string path)
	{
		{
			var moduleFiles = Directory.GetFiles(path, $"*{IniModuleBase.ModuleFileExtension}",
				SearchOption.TopDirectoryOnly);
			var modules = moduleFiles.Select(file => new IniModule(file, this));
			foreach (var module in modules)
			{
				ModulesToHandle.Add(module.Name, module);
			}
		}

		{
			var targetFiles = Directory.GetFiles(path, $"*{IniModuleBase.TargetFileExtension}",
				SearchOption.TopDirectoryOnly);
			var targets = targetFiles.Select(file => new IniTarget(file, this));
			foreach (var target in targets)
			{
				TargetsToHandle.Add(target.Name, target);
			}
		}

		foreach (var directory in Directory.GetDirectories(path))
		{
			ParseInternal(directory);
		}
	}

	public IniModule? GetModule(string name)
	{
		if (!ModulesToHandle.TryGetValue(name, out var module))
		{
			Log.Exception($"cannot find module {name}");
			return null;
		}

		return module;
	}

	public string TargetName { get; }

	private Dictionary<string, IniModule> ModulesToHandle { get; } = new();
	private Dictionary<string, IniTarget> TargetsToHandle { get; } = new();
}