using Bullseye.Internal;
using IniParser.Parser;

namespace ReBuildTool.Internal;


public partial class IniModule
{
    public static readonly string ModuleFileExtension = ".module.ini"; 
    
    public IniModule(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"cannot find module file {path}");
        }
        
        Path = path;
        Name = Path.Substring(0, Path.Length - ModuleFileExtension.Length);
        Sections = new Dictionary<string, Section>();
        
        IniDataParser parser = new();
        var iniData = parser.Parse(File.ReadAllText(path));
        
        foreach (var section in iniData.Sections)
        {
            var newSection = new Section(section.SectionName);
            Sections.Add(section.SectionName, newSection);
            foreach (var key in section.Keys)
            {
                newSection.Properties.Add(key.KeyName, key.Value);
            }
        }
    }
    
    public class Section
    {
        public Section(string name)
        {
            Name = name;
            Properties = new Dictionary<string, string>();
        }
        
        public string Name { get; }
        public Dictionary<string, string> Properties { get; }
    }

    public Dictionary<string, Section> Sections { get; }
    public string Name { get; }
    public string Path { get; }
}