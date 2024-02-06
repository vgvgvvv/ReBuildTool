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