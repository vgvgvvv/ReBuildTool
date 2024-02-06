using System.Reflection;
using ReBuildTool.Common;

namespace ReBuildTool.Internal;

public class BuildActionMeta
{
    public MethodInfo? Method { get; set; }
    public ActionDefineAttribute? Attribute { get; set; }
}

public class ModuleParser
{

    public static void Parse(string path)
    {
        TryInit();
        ParseInternal(path);
    }
    private static void ParseInternal(string path)
    {
        var files = Directory.GetFiles(path, $"*{IniModule.ModuleFileExtension}", SearchOption.TopDirectoryOnly);

        var modules = files.Select(file => new IniModule(file));
        foreach (var module in modules)
        {
            ModulesToHandle.Add(module);
        }
        
        foreach (var directory in Directory.GetDirectories(path))
        {
            ParseInternal(directory);
        }
    }

    public static void InitModules()
    {
        TryInit();
        foreach (var module in ModulesToHandle)
        {
            foreach (var (key, value) in module.Sections)
            {
                // TODO: init modules   
            }
        }
    }

    public static void BuildModules()
    {
        TryInit();
        foreach (var module in ModulesToHandle)
        {
            foreach (var (key, value) in module.Sections)
            {
                // TODO: build modules
            }
        }
    }

    private static bool TryInit()
    {
        if(BuildActionMetas.Count > 0)
        {
            return false;
        }
        
        var types = Assembly.GetExecutingAssembly().GetTypes();
        foreach (var type in types)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(method => method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == typeof(IniModule.Section));
            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<ActionDefineAttribute>();
                if (attr != null)
                {
                    BuildActionMetas.Add(attr.Name, new BuildActionMeta()
                    {
                        Method = method,
                        Attribute = attr
                    });
                }
            }
        }

        return true;
    }
    
    private static Dictionary<string, BuildActionMeta> BuildActionMetas { get; } = new();
    private static List<IniModule> ModulesToHandle { get; } = new();
}