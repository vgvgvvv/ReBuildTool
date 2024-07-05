namespace ReBuildTool.Service.Global;

[AttributeUsage(AttributeTargets.Method)]
public class ActionDefineAttribute : Attribute
{
    public ActionDefineAttribute(string name, string description = "")
    {
        Name = name;
        Description = description;
    }
    
    public string Name { get; }
    
    public string Description { get; }
}

[AttributeUsage(AttributeTargets.Parameter)]
public class ActionParameterAttribute : Attribute
{
    public ActionParameterAttribute(string variableName)
    {
        VariableName = variableName;
    }
    
    public string VariableName { get; }
}