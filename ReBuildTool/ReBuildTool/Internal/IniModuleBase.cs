using System.Diagnostics;
using System.Reflection;
using Bullseye;
using Bullseye.Internal;
using IniParser.Parser;
using ReBuildTool.Common;
using ResetCore.Common;
using ResetCore.Common.Parser;
using ResetCore.Common.Parser.Ini;

namespace ReBuildTool.Internal;


public partial class IniModuleBase
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
        Name = ModulePath.Substring(0, ModulePath.Length - ModuleFileExtension.Length);
        IniFile = IniFile.Parser(ModulePath);
        
    }
    
    public IniFile IniFile { get; }
    public string Name { get; }
    public string ModulePath { get; }
    
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
    static ActionSection()
    {
        TryInit();
    }
    
    public ActionSection(IBuildItem owner, IniFile.Section section) : base(section)
    {
        Owner = owner;
        Actions = section["Actions"]?.List ?? new List<IniFile.SectionItem>();
        if(section["DependOn"] != null)
        {
            var dependenciesSects = section["DependOn"]?.List ?? new List<IniFile.SectionItem>();
            Dependencies = dependenciesSects.Select(sect => sect.Str).ToList();
        }
    }
    
    public string GetTargetName()
    {
        return $"{Owner.GetTargetName()}.{Sect.Name}";
    }

    protected virtual void Run()
    {
        if (Actions != null)
        {
            foreach (var actionSectItem in Actions)
            {
                var actionName = actionSectItem["Name"]?.Str
                    .AssertIfNull("must set name");
                var argsItem = actionSectItem["Args"]?.Map ?? new();
                if (BuildActionMetas.TryGetValue(actionName!, out var actionMeta))
                {
                    if (actionMeta.Method == null)
                    {
                        throw new Exception($"invalid action {actionName}");
                    }

                    var parameters = actionMeta.Method.GetParameters();
                    object?[] args = new object?[parameters.Length];
                    int index = 0;
                    foreach (var parameter in parameters)
                    {
                        if (argsItem.TryGetValue(parameter.Name!, out var argItem))
                        {
                            var arg = argItem.Str.GetValue(parameter.ParameterType);
                            args[index] = arg;
                        }
                        else
                        {
                            do
                            {
                                if (parameter.DefaultValue != null)
                                {
                                    args[index] = parameter.DefaultValue;
                                    break;
                                }

                                var attr = parameter.GetCustomAttribute<ActionParameterAttribute>();
                                if (attr != null)
                                {
                                    if (TargetScope.GetArg(attr.VariableName, out args[index]))
                                    {
                                        break;
                                    }
                                }
                                
                                parameter.ParameterType.GetDefaultValue();

                            } while (false);
                           
                        }
                        index++;
                    }

                    if (CommonCommandGroup.Get().RunDry)
                    {
                        var argStrings = args.Select(arg => arg?.ToString() ?? "null").Join(", ");
                        Log.Info($"{actionMeta.Method.Name}", argStrings);
                    }
                    else
                    {
                        actionMeta.Method?.Invoke(null, args);
                    }
                }
            }
        }
    }
    
    public List<string> GetDependencies()
    {
        return Dependencies?
            .Concat(TargetScope.GetCurrentItemDependOn())
            .ToList() ?? new List<string>();
    }
    
    public void SetupTargets(Targets targets, ref List<string> newTargets)
    {
        var dependOnTargets = new List<string>();
        if (Dependencies != null)
        {
            if (Owner is IniModule iniModule)
            {
                // find from all sections in this module
                foreach (var sectionItem in Dependencies)
                {
                    if (!iniModule.ActionSects.TryGetValue(sectionItem, out var actionSect))
                    {
                        continue;
                    }

                    actionSect.SetupTargets(targets, ref dependOnTargets);
                }
                newTargets.AddRange(dependOnTargets);
            }
        }

        using (var scope = new TargetScope(this))
        {
            var targetaName = GetTargetName();
            scope.AddDependencies(dependOnTargets);
            targets.Add(targetaName, GetDependencies(), Run);
            newTargets.Add(targetaName);
        }
        
    }
    
    public IBuildItem Owner { get; }
    public List<string>? Dependencies { get; }
    public List<IniFile.SectionItem>? Actions { get; }
    
    internal static bool TryInit()
    {
        if (BuildActionMetas.Count > 0)
        {
            return false;
        }

        var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes());
        foreach (var type in types)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
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

    internal static Dictionary<string, BuildActionMeta> BuildActionMetas { get; } = new();
   
}

public class BaseSection
{
    public BaseSection(IniFile.Section section)
    {
        Sect = section;
    }
    
    public IniFile.Section Sect { get; }
}


