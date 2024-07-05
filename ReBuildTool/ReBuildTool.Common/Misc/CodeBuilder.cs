using System.Text;

namespace ReBuildTool.Service.Global;

public class SourceCodeBuilder
{
    public StringBuilder Builder { get; } = new StringBuilder();

    public SourceCodeBuilder AppendLine()
    {
        Builder.AppendLine();
        for (int i = 0; i < Indent; i++)
        {
            Builder.Append("\t");
        }
        return this;
    }
    
    public SourceCodeBuilder AppendLine(string content)
    {
        Builder.AppendLine();
        for (int i = 0; i < Indent; i++)
        {
            Builder.Append("\t");
        }
        return Append(content);
    }

    public static SourceCodeBuilder operator+ (SourceCodeBuilder builder, string content)
    {
        builder.AppendLine(content);
        return builder;
    }
    public static SourceCodeBuilder operator++ (SourceCodeBuilder builder)
    {
        builder.AppendLine();
        return builder;
    }
     

    public SourceCodeBuilder Append(string content)
    {
        Builder.Append(content);
        return this;
    }

    public SourceCodeBuilder Append(char content)
    {
	    Builder.Append(content);
	    return this;
    }

    public SourceCodeBuilder Append(SourceCodeBuilder content)
    {
	    var lines = content.ToString().Replace("\r\n", "\n").Split("\n");
	    bool bIsFirstLine = true;
	    foreach (var line in lines)
	    {
		    if (string.IsNullOrWhiteSpace(line) && bIsFirstLine)
		    {
			    continue;
		    }
		    AppendLine(line);
		    bIsFirstLine = false;
	    }
	    return this;
    }
    
    public SourceCodeBuilder AddIndent(int level = 1)
    {
        Indent += level;
        return this;
    }

    public SourceCodeBuilder RemoveIndent(int level = 1)
    {
        Indent -= level;
        Indent = Math.Max(0, Indent);
        return this;
    }

    public SourceCodeBuilder ResetIndent(int level = 0)
    {
        Indent = level;
        return this;
    }

    public override string ToString()
    {
        return Builder.ToString();
    }

    private int Indent = 0;
}


public class XmlScope : IDisposable
{
    private XmlScope()
    {
    }

    public static XmlScope Create(SourceCodeBuilder builder, string name, params Tuple<string, string>[] args)
    {
        XmlScope scope = new XmlScope();
        scope.scopeName = name;
        scope.codeBuilder = builder;
        
        builder.AppendLine($"<{name}");
        foreach (var (key, value) in args)
        {
            builder.Append($" {key}=\"{value}\"");
        }

        builder.Append(">");
        builder.AddIndent();
        return scope;
    }

    public void Dispose()
    {
        codeBuilder.RemoveIndent();
        codeBuilder.AppendLine($"</{scopeName}>");
    }

    private string scopeName;
    private SourceCodeBuilder codeBuilder;
}


public class XmlCodeBuilder : SourceCodeBuilder
{
    public XmlScope CreateXmlScope(string name, params Tuple<string, string>[] args)
    {
        return XmlScope.Create(this, name, args);
    }

    public XmlCodeBuilder WriteHeader()
    {
        Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        return this;
    }

    public XmlCodeBuilder WriteNode(string key, string value, params Tuple<string, string>[] args)
    {
        AppendLine($"<{key}");
        foreach (var (k, v) in args)
        {
            Append($" {k}=\"{v}\"");
        }
        Append($">{value}</{key}>");
        return this;
    }

    public XmlCodeBuilder WriteNodeWithoutValue(string key, params Tuple<string, string>[] args)
    {
        AppendLine($"<{key}");
        foreach (var (k, v) in args)
        {
            Append($" {k}=\"{v}\"");
        }
        Append($"/>");
        return this;
    }
}