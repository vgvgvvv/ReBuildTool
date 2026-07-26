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

        if (output.Exists() && output.ReadAllText() == codeBuilder.ToString())
        {
            Log.Info("Makefile is not changed, skip writing");
            return;
        }

        output.WriteAllText(codeBuilder.ToString());
    }

    private void FlushTarget(SourceCodeBuilder builder, Target target)
    {
        var dependencies = target.Dependencies.Select(MakeTargetName);
        builder.AppendLine($"{MakeTargetName(target.Name)} : {string.Join(" ", dependencies)}");
        builder.AddIndent();
        foreach (var invocation in target.Invocations)
        {
            builder.AppendLine($"\t{MakeRecipe(invocation)}");
        }
        builder.RemoveIndent();
    }

    /// <summary>
    /// Make-level escaping for a target or prerequisite name. The caller has already made the
    /// path safe for the shell (see <c>CppBuilder.CompileProcess.MakeSureValidPath</c>); this
    /// adds what make itself needs on top: <c>$</c> doubled so make does not read it as a
    /// variable reference, and <c>#</c> backslash-escaped because it starts a comment in this
    /// position (unlike in a recipe line, where make hands it to the shell).
    /// </summary>
    private static string MakeTargetName(string name)
    {
        return name.Replace("$", "$$").Replace("#", "\\#");
    }

    /// <summary>
    /// Make-level escaping for a recipe line. The line is an already shell-quoted command, so
    /// only make's own expansion has to be neutralised: a literal <c>$</c> must be written
    /// <c>$$</c> or make expands it (typically to nothing) before the shell ever sees it.
    /// </summary>
    private static string MakeRecipe(string invocation)
    {
        return invocation.Replace("$", "$$");
    }

    public List<Target> Targets { get; } = new();
}