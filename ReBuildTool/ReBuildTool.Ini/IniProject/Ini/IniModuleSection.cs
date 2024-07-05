using System.Reflection;
using Bullseye;
using ReBuildTool.Service.Global;
using ResetCore.Common;
using ResetCore.Common.Parser.Ini;

namespace ReBuildTool.Internal.Ini;

public class InitSection : ActionSection
{
    public InitSection(IniModuleBase owner, IniFile.Section section) : base(owner, section)
    {
    }

}

public class BuildSection : ActionSection
{
    public BuildSection(IniModuleBase owner, IniFile.Section section) : base(owner, section)
    {
    }
    
}

public class ActionSection : BaseSection, ITargetItem
{
    static ActionSection()
    {
        TryInit();
    }
    
    public ActionSection(IniModuleBase owner, IniFile.Section section) : base(owner, section)
    {
        Actions = section["Actions"]?.List ?? new List<IniFile.SectionItem>();
        if(section["DependOn"] != null)
        {
            var dependenciesSects = section["DependOn"]?.List ?? new List<IniFile.SectionItem>();
            Dependencies = dependenciesSects.Select(sect =>
            {
                if (Owner.IniFile[sect.Str] != null)
                {
                    return $"{Owner.Name}.{sect.Str}";
                }
                return sect.Str;
            }).ToList();
        }
    }

    public string Name => Sect.Name;
    
    protected virtual Action GetTask()
    {
        if (Actions == null)
        {
            Log.Info("no actions to run");
            return () => {};
        }

        var subActions = new List<Action>();
        foreach (var actionSectItem in Actions)
        {
            var actionName = actionSectItem["Name"]?.Str
                .AssertIfNull("must set name");
            var argsItem = actionSectItem["Args"]?.Map ?? new();
            if (!BuildActionMetas.TryGetValue(actionName!, out var actionMeta)) 
                continue;
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
                        if (parameter.HasDefaultValue)
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
                subActions.Add(() =>
                {
                    var argStrings = args.Select(arg => arg?.ToString() ?? "null").Join(", ");
                    Log.Info($"{actionMeta.Method.Name}", argStrings);
                });
            }
            else
            {
                subActions.Add(() =>
                {
                    actionMeta.Method?.Invoke(null, args);
                });
            }
        }

        return () =>
        {
            foreach (var action in subActions)
            {
                action();
            }
        };
    }
    
    public List<string> GetDependencies()
    {
        return Dependencies?
            .Concat(TargetScope.GetCurrentItemDependOn())
            .ToList() ?? new List<string>();
    }
    
    public void SetupTargets(Targets targets, ref List<string> newTargets, string prefix = "")
    {
        var dependOnTargets = new List<string>();
        if (Dependencies != null)
        {
            Owner.SetupActionSects(targets, Dependencies, out dependOnTargets);
            newTargets.AddRange(dependOnTargets);
        }

        using (var scope = new TargetScope(this))
        {
            var targetName = FullName;
            scope.AddDependencies(dependOnTargets);
            var task = GetTask();
            if (!string.IsNullOrEmpty(prefix))
            {
                targetName = $"{prefix}-{targetName}";
            }
            targets.Add(targetName, $"Depended By {GetDependencies().Join(", ")}", 
                GetDependencies(), task);
            newTargets.Add(targetName);
        }
        
    }
    
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
    public BaseSection(IniModuleBase owner, IniFile.Section section)
    {
        Owner = owner;
        Sect = section;
    }

    public string FullName => $"{Owner.Name}.{Sect.Name}";
    
    public IniModuleBase Owner { get; }
    public IniFile.Section Sect { get; }
}