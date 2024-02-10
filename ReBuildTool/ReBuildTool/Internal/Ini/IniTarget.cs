using Bullseye;
using ResetCore.Common;
using ResetCore.Common.Parser.Ini;

namespace ReBuildTool.Internal.Ini;

public class IniTarget : IniModuleBase, IBuildItem
{
	public IniTarget(string modulePath, ModuleProject owner) : base(modulePath, owner)
	{
		var targetSection = IniFile["Target"]
			.AssertIfNull($"{IniFile.FilePath} : Target section not found");
		TargetSect = new TargetSection(targetSection);
		
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

	public class TargetSection 
	{
		public TargetSection(IniFile.Section section)
		{
			var entries = section["Entries"]?.List;
			Entries = entries?.Select(item => item.Str).ToList() ?? new List<string>();
		}
		public List<string> Entries { get; }
	}
    
	public void SetupInitTargets(Targets targets, ref List<string> newTargets)
	{
		using (var scope = new TargetScope(this))
		{
			// init target first
			if (InitSect != null)
			{
				var initSectTargets = new List<string>();
				InitSect.SetupTargets(targets, ref initSectTargets);
				scope.AddDependencies(initSectTargets);
				newTargets.AddRange(initSectTargets);
			}
			// then setup all sub modules
			foreach (var targetEntry in TargetSect.Entries)
			{
				var module = Owner.GetModule(targetEntry);
				if (module != null)
				{
					var entriesTargets = new List<string>();
					module.SetupInitTargets(targets, ref entriesTargets);
					newTargets.AddRange(entriesTargets);
				}
			}
		}
	}

	public void SetupBuildTargets(Targets targets, ref List<string> newTargets)
	{
		using (var scope = new TargetScope(this))
		{
			if (BuildSect != null)
			{
				var buildSectTargets = new List<string>();
				BuildSect.SetupTargets(targets, ref buildSectTargets);
				scope.AddDependencies(buildSectTargets);
				newTargets.AddRange(buildSectTargets);
			}
			foreach (var targetEntry in TargetSect.Entries)
			{
				var module = Owner.GetModule(targetEntry);
				if (module != null)
				{
					var entriesTargets = new List<string>();
					module.SetupBuildTargets(targets, ref entriesTargets);
					newTargets.AddRange(entriesTargets);
				}
			}
		}
	}

	public InitSection? InitSect { get; }
	public BuildSection? BuildSect { get; }
	public TargetSection TargetSect { get; }

}