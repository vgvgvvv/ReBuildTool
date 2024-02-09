using Bullseye;
using ResetCore.Common.Parser.Ini;

namespace ReBuildTool.Internal;


public abstract class IniModuleBase
{
    public static readonly string ModuleFileExtension = ".module.ini"; 
    
    public static readonly string TargetFileExtension = ".target.ini"; 
    
    public IniModuleBase(string modulePath, ModuleProject owner)
    {
        if (!File.Exists(modulePath))
        {
            throw new FileNotFoundException($"cannot find module file {modulePath}");
        }
        
        ModulePath = modulePath;
        Owner = owner;
        var fileName = Path.GetFileName(ModulePath);
        Name = fileName.Substring(0, fileName.Length - ModuleFileExtension.Length);
        IniFile = IniFile.Parser(ModulePath);
        
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
    public string Name { get; }
    public string ModulePath { get; }
    
    public ModuleProject Owner { get; }
    
    public Dictionary<string, ActionSection> ActionSects { get; } = new();


}


