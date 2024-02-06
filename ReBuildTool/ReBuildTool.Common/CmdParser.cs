using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Common;

namespace ReBuildTool.Common;

internal class CommandArgInfo
{
    public string? Help => Attribute.Description;
    public FieldInfo? Field { get; set; }
    public PropertyInfo? Property { get; set; }
    public CmdLineAttribute Attribute { get; set; }

    public bool IsSet { get; private set; } = false;

    public void MarkHasSet()
    {
        IsSet = true;
    }
    
    public Type? DeclaringType
    {
        get
        {
            if (Field != null)
            {
                return Field.DeclaringType;
            }
            else if(Property != null)
            {
                return Property.DeclaringType;
            }
           
            throw new Exception("Invalid CommandArgInfo");
        }
    }

    public Type SelfType
    {
        get
        {
            if (Field != null)
            {
                return Field.FieldType;
            }
            else if(Property != null)
            {
                return Property.PropertyType;
            };
            
            throw new Exception("Invalid CommandArgInfo");
        }
    }

    public string Name
    {
        get
        {
            if (Field != null)
            {
                return Field.Name;
            }
            else if(Property != null)
            {
                return Property.Name;
            }

            throw new Exception("Invalid CommandArgInfo");
        }
    }

    public void SetValue(object target, object value)
    {
        if (Field != null)
        {
            Field.SetValue(target, value);
        }
        else if(Property != null)
        {
            Property.SetValue(target, value);
        }
    }
}

public class CmdParser
{

    public static T? Get<T>() where T : CommandLineArgGroup
    {
        return CmdLineArgs.GetValueOrDefault(typeof(T)) as T;
    }

    public static void ShowHelp()
    {
        TryInit();
        foreach (var (key, value) in CmdLineArgMeta)
        {
            var help = value.Help;
            if (help != null)
            {
                Log.Info("CmdParser", $"--{key} ({value.SelfType.Name}): {help}");
            }
        }
    }
    
    public static void Parse()
    {
        TryInit();   
        var args = Environment.GetCommandLineArgs();
        if (args.Length == 1)
        {
            ShowHelp();
            return;
        }
        for (var i = 1; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.StartsWith("--"))
            {
                var key = arg.Substring(2).ToLower();
                if (!CmdLineArgMeta.TryGetValue(key, out var field))
                {
                    Log.Warning("CmdParser", $"invalid arg : {key}");
                    ShowHelp();
                    return;
                }
                field.MarkHasSet();

                var type = field.DeclaringType ?? throw new Exception($"cannot find declaring type for {field.Name}");
                Debug.Assert(type.IsAssignableFrom(typeof(CommandLineArgGroup)));
                if (!CmdLineArgs.TryGetValue(type, out var group))
                {
                    group = Activator.CreateInstance(type) as CommandLineArgGroup;
                    Debug.Assert(group != null);
                    CmdLineArgs.Add(type, group);
                }
                if (!args[++i].StartsWith("--"))
                {
                    var value = args[++i];
                    
                    field.SetValue(group, value.GetValue(field.SelfType));
                }
                else
                {
                    if (field.SelfType == typeof(bool))
                    {
                        field.SetValue(group, true);
                    }
                    else
                    {
                        var value = field.SelfType.GetDefaultValue();
                        if (value != null)
                        {
                            field.SetValue(group, value);
                        }
                    }
                }
            }
        }
    }
    

    private static bool TryInit()
    {
        if(CmdLineArgMeta.Count > 0)
            return false;
        
        var types = Assembly.GetExecutingAssembly().GetTypes();
        foreach (var type in types)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                var attr = field.GetCustomAttribute<CmdLineAttribute>();
                if (attr != null)
                {
                    CmdLineArgMeta.Add(field.Name.ToLower(), new CommandArgInfo()
                    {
                        Attribute = attr,
                        Field = field
                    });
                }
            }
            
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(prop=>prop.GetMethod != null);
            
            foreach (var property in properties)
            {
                var attr = property.GetCustomAttribute<CmdLineAttribute>();
                if (attr != null)
                {
                    CmdLineArgMeta.Add(property.Name.ToLower(), new CommandArgInfo()
                    {
                        Attribute = attr,
                        Property = property
                    });
                }
            }
        }

        return true;
    }

    private static Dictionary<Type, CommandLineArgGroup> CmdLineArgs { get; } = new();
    private static Dictionary<string, CommandArgInfo> CmdLineArgMeta { get; } = new();
}

public class CommandLineArgGroup
{
    
}

public class CommandLineArgGroup<T> : CommandLineArgGroup where T : CommandLineArgGroup
{
    public static T Get()
    {
        return CmdParser.Get<T>() ?? throw new Exception($"cannot find CommandLineArgGroup: {typeof(T).Name}");
    }
}