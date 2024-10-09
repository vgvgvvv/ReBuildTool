using NiceIO;
using ReBuildTool.Service.Global;

using ResetCore.Common;

namespace ReBuildTool.ToolChain;

public class MakeFileGenerator
{
    public enum TargetType
    {
        MainTarget,
        SubTarget
    }
    public class Target
    {
        public Target(string name, TargetType type)
        {
            Name = name;
            Type = type;
        }
        public TargetType Type = TargetType.SubTarget;
        public string Name;
        public List<string> Dependencies { get; } = new();
        public List<string> Invocations { get; } = new();
    }

    public void FlushToFile(NPath output)
    {
        var codeBuilder = new SourceCodeBuilder();
        var mainTargets = Targets.Where(t => t.Type == TargetType.MainTarget).ToList();
        var subTargets = Targets.Where(t => t.Type == TargetType.SubTarget).ToList();
        
        foreach (var target in mainTargets)
        {
            FlushTarget(codeBuilder, target);
        }
        
        foreach (var target in subTargets)
        {
            FlushTarget(codeBuilder, target);
        }

        if (output.ReadAllText() == codeBuilder.ToString())
        {
            Log.Info("Makefile is not changed, skip writing");
            return;
        }

        output.WriteAllText(codeBuilder.ToString());
    }

    private void FlushTarget(SourceCodeBuilder builder, Target target)
    {
        builder.AppendLine($"{target.Name} : {string.Join(" ", target.Dependencies)}");
        builder.AddIndent();
        foreach (var invocation in target.Invocations)
        {
            builder.AppendLine($"\t{invocation}");
        }
        builder.RemoveIndent();
    }
    
    
    
    public List<Target> Targets { get; } = new();
}