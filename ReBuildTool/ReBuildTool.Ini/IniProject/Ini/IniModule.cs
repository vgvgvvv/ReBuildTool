using Bullseye;
using ReBuildTool.Actions;
using ResetCore.Common;
using ResetCore.Common.Parser.Ini;

namespace ReBuildTool.IniProject.Ini;

public class IniModule : IniModuleBase, IBuildItem
{
	public IniModule(string modulePath, ModuleProject owner) : base(modulePath, owner)
	{
		var targetSection = IniFile["Module"]
			.AssertIfNull($"{IniFile.FilePath} : Module section not found");
		ModuleSect = new ModuleSection(this, targetSection);

		var initSection = IniFile["Init"];
		if (initSection != null)
		{
			InitSect = new InitSection(this, initSection);
		}
		
		var buildSection = IniFile["Build"];
		if(buildSection != null)
		{
			BuildSect = new BuildSection(this, buildSection);
		}
		
		foreach (var (key, value) in IniFile)
		{
			if (key.StartsWith("Action:"))
			{
				var actionSect = new ActionSection(this, value);
				ActionSects.Add(actionSect.FullName, actionSect);
			}
		}
	}

	public class ModuleSection : BaseSection
	{
		public ModuleSection(IniModuleBase owner, IniFile.Section section) : base(owner, section)
		{
			var dependencies = section["Dependencies"]?.List;
			Dependencies = dependencies?
				.Select(item =>
				{
					if (item.ItemType == IniFile.SectionItem.SectionItemType.Map)
					{
						var moduleName = item["Name"]
							.AssertIfNull($"must provide module name in {TargetScope.GetCurrentItemName()}");
						var remakeUrl = item["ReMakeUrl"];
						if (remakeUrl != null)
						{
							Log.Info($"install {moduleName.Str} from {remakeUrl.Str}");
							ReMake.InstallReMakeLibrary(remakeUrl.Str, moduleName.Str);
						}

						return moduleName.Str;
					}
					else if(item.ItemType == IniFile.SectionItem.SectionItemType.String)
					{
						return item.Str;
					}
					else
					{
						Log.Exception("invalid module dependency");
						return string.Empty;
					}
				})
				.Where(moduleName => !string.IsNullOrEmpty(moduleName))
				.ToList() ?? new List<string>();
		}
		public List<string> Dependencies { get; }

	}
    
	// init all dependencies then init self
	public void SetupInitTargets(Targets targets, ref List<string> newTargets)
	{
		
		var dependOnTargets = new List<string>();
		foreach (var dependency in ModuleSect.Dependencies)
		{
			var module = Owner.GetModule(dependency);
			if (module != null)
			{
				module.SetupInitTargets(targets, ref dependOnTargets);
			}
			newTargets.AddRange(dependOnTargets);
		}

		using (var scope = new TargetScope(this))
		{
			// setup self
			if (InitSect != null)
			{
				var currentModuleTargets = new List<string>();
				scope.AddDependencies(dependOnTargets);
				scope.SetArg("WorkDirectory", Path.GetDirectoryName(IniFile.FilePath)!);
				InitSect.SetupTargets(targets, ref currentModuleTargets);
				newTargets.AddRange(currentModuleTargets);
			}
		}
		
	}

	// build all dependencies first and then build self
	public void SetupBuildTargets(Targets targets, ref List<string> newTargets)
	{
		var dependOnTargets = new List<string>();
		foreach (var dependency in ModuleSect.Dependencies)
		{
			var module = Owner.GetModule(dependency);
			if (module != null)
			{
				module.SetupBuildTargets(targets, ref dependOnTargets);
			}
			newTargets.AddRange(dependOnTargets);
		}
			
		// build self
		using (var scope = new TargetScope(this))
		{
			if (BuildSect != null)
			{
				var currentModuleTargets = new List<string>();
				scope.AddDependencies(dependOnTargets);
				BuildSect.SetupTargets(targets, ref currentModuleTargets);
				newTargets.AddRange(currentModuleTargets);
			}
		}
	}
	
	public InitSection? InitSect { get; }
	public BuildSection? BuildSect { get; }
	public ModuleSection ModuleSect { get; }

}
