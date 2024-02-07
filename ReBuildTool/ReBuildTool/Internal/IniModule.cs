using System.Diagnostics;
using Bullseye.Internal;
using IniParser.Parser;
using ResetCore.Common.Parser;
using ResetCore.Common.Parser.Ini;

namespace ReBuildTool.Internal;


public partial class IniModule
{
    public static readonly string ModuleFileExtension = ".module.ini"; 
    
    public static readonly string TargetFileExtension = ".target.ini"; 
    
    public IniModule(string path)
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