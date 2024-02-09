using Bullseye;
using ResetCore.Common;
using ResetCore.Common.Parser.Ini;

namespace ReBuildTool.Internal;

public class IniTarget : IniModuleBase, IBuildItem
{
	public IniTarget(string path, ModuleProject owner) : base(path, owner)
	{
		var targetSection = IniFile["Target"]
			.AssertIfNull("Target section not found");
		TargetSect = new TargetSection(targetSection);
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
    
	public void SetupInitTargets(Targets targets)
	{
		using (var scope = new TargetScope(this))
		{
			// init target first
			if (InitSect != null)
			{
				InitSect.SetupTargets(targets, out var newTargets);
				scope.AddDependencies(newTargets);
			}
			// then setup all sub modules
			foreach (var targetEntry in TargetSect.Entries)
			{
				var module = Owner.GetModule(targetEntry);
				if (module != null)
				{
					module.SetupInitTargets(targets);
				}
			}
		}
	}

	public void SetupBuildTargets(Targets targets)
	{
		using (var scope = new TargetScope(this))
		{
			if (BuildSect != null)
			{
				BuildSect.SetupTargets(targets, out var newTargets);
				scope.AddDependencies(newTargets);
			}
			foreach (var targetEntry in TargetSect.Entries)
			{
				var module = Owner.GetModule(targetEntry);
				if (module != null)
				{
					module.SetupBuildTargets(targets);
				}
			}
		}
	}

	public string GetTargetName()
	{
		return TargetScope.GetCurrentItemName();
	}

	public InitSection? InitSect { get; }
	public BuildSection? BuildSect { get; }
	public TargetSection TargetSect { get; }
}