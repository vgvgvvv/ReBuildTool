using Bullseye;
using ResetCore.Common;
using ResetCore.Common.Parser.Ini;

namespace ReBuildTool.Internal;

public class IniModule : IniModuleBase, IBuildItem
{
	public IniModule(string path, ModuleProject owner) : base(path, owner)
	{
		var targetSection = IniFile["Modules"]
			.AssertIfNull("Modules section not found");
		ModuleSect = new ModuleSection(targetSection);
	}
    
    
	public class ModuleSection
	{
		public ModuleSection(IniFile.Section section)
		{
			var dependencies = section["Dependencies"]?.List;
			Dependencies = dependencies?
				.Select(item => item.Str)
				.ToList() ?? new List<string>();
		}
		public List<string> Dependencies { get; }

	}
    
	public void SetupInitTargets(Targets targets)
	{
		using (var scope = new TargetScope(this))
		{
			// TODO: init dependencies
			
			// setup self
			if (InitSect != null)
			{
				InitSect.SetupTargets(targets, out var newTargets);
			}
		}
	}

	public void SetupBuildTargets(Targets targets)
	{
		// TODO: build dependencies
			
		// build self
		using (var scope = new TargetScope(this))
		{
			if (BuildSect != null)
			{
				BuildSect.SetupTargets(targets, out var newTargets);
			}
		}
	}
	
	public string GetTargetName()
	{
		return TargetScope.GetCurrentItemName();
	}
    
	public InitSection? InitSect { get; }
	public BuildSection? BuildSect { get; }
	public ModuleSection ModuleSect { get; }
	public Dictionary<string, ModuleSection> ActionSects { get; }
	
}
