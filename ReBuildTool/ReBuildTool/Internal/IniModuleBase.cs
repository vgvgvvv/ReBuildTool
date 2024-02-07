using System.Diagnostics;
using Bullseye.Internal;
using IniParser.Parser;
using ResetCore.Common.Parser;
using ResetCore.Common.Parser.Ini;

namespace ReBuildTool.Internal;


public partial class IniModuleBase
{
    public static readonly string ModuleFileExtension = ".module.ini"; 
    
    public static readonly string TargetFileExtension = ".target.ini"; 
    
    public IniModuleBase(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"cannot find module file {path}");
        }
        
        Path = path;
        Name = Path.Substring(0, Path.Length - ModuleFileExtension.Length);
        IniFile = IniFile.Parser(Path);
    }
    
    public IniFile IniFile { get; }
    public string Name { get; }
    public string Path { get; }
}

public class IniModule : IniModuleBase
{
    public IniModule(string path) : base(path)
    {
        var targetSection = IniFile.Sections.GetValueOrDefault("Module")
                            ?? throw new Exception($"Module section not found : {Path}");
        var dependencies = targetSection.Properties.GetValueOrDefault("Dependencies")?.List
                        ?? throw new Exception("Entry section not found");
        Dependencies = dependencies.Select(item => item.Str).ToList();
    }
    
    public List<string> Dependencies { get; }
}

public class IniTarget : IniModuleBase
{
    public IniTarget(string path) : base(path)
    {
        var targetSection = IniFile.Sections.GetValueOrDefault("Target")
                            ?? throw new Exception($"Target section not found : {Path}");
        var entryList = targetSection.Properties.GetValueOrDefault("Entries")?.List
                        ?? throw new Exception("Entry section not found");
        Entries = entryList.Select(item => item.Str).ToList();
    }
    public List<string> Entries { get; }
}