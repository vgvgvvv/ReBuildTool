using System.Reflection;
using ReBuildTool.Common;
using ResetCore.Common.Parser.Ini;

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
        {
            var moduleFiles = Directory.GetFiles(path, $"*{IniModule.ModuleFileExtension}", SearchOption.TopDirectoryOnly);
            var modules = moduleFiles.Select(file => new IniModule(file));
            foreach (var module in modules)
            {
                ModulesToHandle.Add(module);
            }
        }

        {
            var targetFiles = Directory.GetFiles(path, $"*{IniModule.TargetFileExtension}", SearchOption.TopDirectoryOnly);
            var targets = targetFiles.Select(file => new IniModule(file));
            foreach (var target in targets)
            {
                TargetsToHandle.Add(target);
            }
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
            
        }
    }

    public static void BuildModules()
    {
        TryInit();
        foreach (var module in ModulesToHandle)
        {
          
        }
    }

    private static bool TryInit()
    {
        if(BuildActionMetas.Count > 0)
        {
            return false;
        }
        
        var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes());
        foreach (var type in types)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(method => method.GetParameters().Length == 1 && method.GetParameters()[0].ParameterType == typeof(IniFile.Section));
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
    
    private static List<IniModule> TargetsToHandle { get; } = new();
}