using System.Diagnostics;
using Bullseye;
using Bullseye.Internal;
using IniParser.Parser;
using ResetCore.Common;
using ResetCore.Common.Parser;
using ResetCore.Common.Parser.Ini;

namespace ReBuildTool.Internal;


public partial class IniModuleBase
{
    public static readonly string ModuleFileExtension = ".module.ini"; 
    
    public static readonly string TargetFileExtension = ".target.ini"; 
    
    public IniModuleBase(string path, ModuleProject owner)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"cannot find module file {path}");
        }
        
        Path = path;
        Owner = owner;
        Name = Path.Substring(0, Path.Length - ModuleFileExtension.Length);
        IniFile = IniFile.Parser(Path);
        
    }
    
    public IniFile IniFile { get; }
    public string Name { get; }
    public string Path { get; }
    
    public ModuleProject Owner { get; }
}

public class InitSection : ActionSection
{
    public InitSection(IBuildItem owner, IniFile.Section section) : base(owner, section)
    {
    }

}

public class BuildSection : ActionSection
{
    public BuildSection(IBuildItem owner, IniFile.Section section) : base(owner, section)
    {
    }
    
}

public class ActionSection : BaseSection, ITargetItem
{
    public ActionSection(IBuildItem owner, IniFile.Section section) : base(section)
    {
        Owner = owner;
        Actions = section["Actions"]?.List ?? new List<IniFile.SectionItem>();
        Dependencies = section["DependOn"]?.List ?? new List<IniFile.SectionItem>();
    }
    
    public string GetTargetName()
    {
        return $"{Owner.GetTargetName()}.{Sect.Name}";
    }

    protected virtual void Run()
    {
        
    }
    
    public List<string> GetDependencies()
    {
        return Dependencies?.Select(item => item.Str)
            .Concat(TargetScope.GetCurrentItemDependOn())
            .ToList() ?? new List<string>();
    }
    
    public void SetupTargets(Targets targets, out List<string> newTargets)
    {
        var targetaName = GetTargetName();
        targets.Add(targetaName, GetDependencies(), Run);
        newTargets = new List<string>()
        {
            targetaName
        };
    }
    
    public IBuildItem Owner { get; }
    public List<IniFile.SectionItem>? Dependencies { get; }
    public List<IniFile.SectionItem>? Actions { get; }
   
}

public class BaseSection
{
    public BaseSection(IniFile.Section section)
    {
        Sect = section;
    }
    
    public IniFile.Section Sect { get; }
}


