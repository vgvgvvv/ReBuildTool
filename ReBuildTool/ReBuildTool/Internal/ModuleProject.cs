using System.Reflection;
using ReBuildTool.Common;
using ResetCore.Common.Parser.Ini;

namespace ReBuildTool.Internal;

public class BuildActionMeta
{
    public MethodInfo? Method { get; set; }
    public ActionDefineAttribute? Attribute { get; set; }
}

public class ModuleProject
{

    public static void Parse(string path)
    {
        TryInit();
        ParseInternal(path);
    }
    private static void ParseInternal(string path)
    {
        {
            var moduleFiles = Directory.GetFiles(path, $"*{IniModuleBase.ModuleFileExtension}", SearchOption.TopDirectoryOnly);
            var modules = moduleFiles.Select(file => new IniModule(file));
            foreach (var module in modules)
            {
                ModulesToHandle.Add(module.Name, module);
            }
        }

        {
            var targetFiles = Directory.GetFiles(path, $"*{IniModuleBase.TargetFileExtension}", SearchOption.TopDirectoryOnly);
            var targets = targetFiles.Select(file => new IniTarget(file));
            foreach (var target in targets)
            {
                TargetsToHandle.Add(target.Name, target);
            }
        }
        
        foreach (var directory in Directory.GetDirectories(path))
        {
            ParseInternal(directory);
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
    private static Dictionary<string, IniModule> ModulesToHandle { get; } = new();
    private static Dictionary<string, IniTarget> TargetsToHandle { get; } = new();
}