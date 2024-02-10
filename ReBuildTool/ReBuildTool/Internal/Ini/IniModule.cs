using Bullseye;
using ResetCore.Common;
using ResetCore.Common.Parser.Ini;

namespace ReBuildTool.Internal.Ini;

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
				.Select(item => item.Str)
				.ToList() ?? new List<string>();
		}
		public List<string> Dependencies { get; }

	}
    
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
