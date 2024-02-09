using System.Reflection;
using Bullseye;
using ReBuildTool.Common;
using ResetCore.Common;
using ResetCore.Common.Parser.Ini;

namespace ReBuildTool.Internal;

public class BuildActionMeta
{
	public MethodInfo? Method { get; set; }
	public ActionDefineAttribute? Attribute { get; set; }
}

public interface IBuildItem : ITargetItem
{
	public void SetupInitTargets(Targets targets);
	public void SetupBuildTargets(Targets targets);
}

public interface ITargetItem
{
	public string GetTargetName();
}

public static class BuildItemExtension
{
	public static string GetInitTaskName(this IBuildItem item)
	{
		return $"{item.GetTargetName()}.Setup";
	}

	public static string GetBuildTaskName(this IBuildItem item)
	{
		return $"{item.GetTargetName()}.Build";
	}
}

internal class ModuleProjectMeta
{
	internal static bool TryInit()
	{
		if (BuildActionMetas.Count > 0)
		{
			return false;
		}

		var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes());
		foreach (var type in types)
		{
			var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
				.Where(method =>
					method.GetParameters().Length == 1 &&
					method.GetParameters()[0].ParameterType == typeof(IniFile.Section));
			foreach (var method in methods)
			{
				var attr = method.GetCustomAttribute<ActionDefineAttribute>();
				if (attr != null)
				{
					BuildActionMetas.Add(attr.Name, new BuildActionMeta()
					{
						Method = method,
						Attribute = attr
					});
				}
			}
		}

		return true;
	}

	internal static Dictionary<string, BuildActionMeta> BuildActionMetas { get; } = new();
}

internal class TargetScope : IDisposable
{
	public TargetScope(IBuildItem item)
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
	
	public TargetScope AddDependencies(List<string>? dependency)
	{
		if (dependency != null)
		{
			CurrentItem.DependOn.AddRange(dependency);
		}

		return this;
	}

	public void Dispose()
	{
		ScopeStack.Pop();
	}

	public static string GetCurrentItemName()
	{
		return string.Join(".", ScopeStack.Select(item => item.Item.GetTargetName()));
	}
	
	public static List<string> GetCurrentItemDependOn()
	{
		return ScopeStack.SelectMany(item => item.DependOn).ToList();
	}

	public class TargetScopeItem
	{
		public IBuildItem Item;
		public List<string> DependOn;

		public TargetScopeItem(IBuildItem item, List<string>? dependOn = null)
		{
			Item = item;
			DependOn = dependOn ?? new List<string>();
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
		ModuleProjectMeta.TryInit();
		return new ModuleProject(target);
	}

	public ModuleProject Parse(string path)
	{
		ModuleProjectMeta.TryInit();
		ParseInternal(path);
		return this;
	}

	public string GetTargetName()
	{
		return "ProjectRoot";
	}

	public void SetupInitTargets(Targets targets)
	{
		if (!TargetsToHandle.TryGetValue(TargetName, out var targetToHandle))
		{
			Log.Exception($"cannot find target {TargetName}");
			return;
		}

		using (new TargetScope(this))
		{
			targetToHandle.SetupInitTargets(targets);
		}
	}

	public void SetupBuildTargets(Targets targets)
	{
		if (!TargetsToHandle.TryGetValue(TargetName, out var targetToHandle))
		{
			Log.Exception($"cannot find target {TargetName}");
			return;
		}

		using (new TargetScope(this))
		{
			targetToHandle.SetupBuildTargets(targets);
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