namespace ReBuildTool.Common;

[AttributeUsage(AttributeTargets.Method)]
public class ActionDefineAttribute : Attribute
{
    public ActionDefineAttribute(string name)
    {
        Name = name;
    }
    
    public string Name { get; }
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