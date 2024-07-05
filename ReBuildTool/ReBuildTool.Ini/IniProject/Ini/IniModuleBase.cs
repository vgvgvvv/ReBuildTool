using Bullseye;
using ResetCore.Common;
using ResetCore.Common.Parser.Ini;

namespace ReBuildTool.IniProject.Ini;


public abstract class IniModuleBase : ModuleBase
{
    public static readonly string StaticModuleFileExtension = ".module.ini";
    public static readonly string StaticTargetFileExtension = ".target.ini";
    public override string ModuleFileExtension => StaticModuleFileExtension; 
    public override string TargetFileExtension => StaticTargetFileExtension; 
    
    
    public IniModuleBase(string modulePath, ModuleProject owner) : base(modulePath, owner)
    {
        if (!File.Exists(modulePath))
        {
            throw new FileNotFoundException($"cannot find module file {modulePath}");
        }

        try
        {
            IniFile = IniFile.Parse(ModulePath);
        }
        catch (Exception e)
        {
            Log.Exception(e);
            Environment.Exit(1);
        }
    }

    public void SetupActionSects(Targets targets, List<string> actionSects, out List<string> newTargets)
    {
        newTargets = new List<string>();
        // find from all sections in this module
        foreach (var sectionItem in actionSects)
        {
            if (!ActionSects.TryGetValue(sectionItem, out var actionSect))
            {
                continue;
            }
            actionSect.SetupTargets(targets, ref newTargets);
        }
    }
    
    public IniFile IniFile { get; }
    public Dictionary<string, ActionSection> ActionSects { get; } = new();


}


