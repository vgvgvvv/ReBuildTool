namespace ReBuildTool.Common;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class CmdLineAttribute : Attribute
{
    public CmdLineAttribute(string description, bool required = false)
    {
        Description = description;
        Required = required;
    }

    
    public bool Required { get; }
    public string Description { get; }
}