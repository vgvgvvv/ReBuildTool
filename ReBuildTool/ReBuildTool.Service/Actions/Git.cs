using NiceIO;
using ReBuildTool.Service.Global;
using ReBuildTool.Service.CommandGroup;
using ResetCore.Common;

namespace ReBuildTool.Actions;

public class Git
{
    public static NPath GitDir => GlobalPaths.IntermediaPath.Combine("Git").EnsureDirectoryExists();
    
    [ActionDefine("Git.Get")]
    public static void GetFromGit(string url, 
        string name,
        string targetPath,
        string? tag = null)
    {
        if (string.IsNullOrEmpty(targetPath))
        {
            targetPath = GitDir;
        }
        var gitRepoTargetPath = Path.Combine(targetPath, name);
        if (!Directory.Exists(gitRepoTargetPath))
        {
            var args = new List<string>();
            args.Add("clone");
            args.Add(url);
            args.Add(gitRepoTargetPath);


            if (!string.IsNullOrEmpty(tag))
            {
                args.Add("--branch");
                args.Add(tag);
            }

            Log.Info("Run Git: ", "git", args.Join(" "));
            SimpleExec.Command.Run("git", args.Join(" "), targetPath);
        }
        else
        {
            var args = new List<string>();
            args.Add("pull");
            
            Log.Info("Run Git: ", "git", args.Join(" "));
            SimpleExec.Command.Run("git", "reset --hard", gitRepoTargetPath);
            SimpleExec.Command.Run("git", args.Join(" "), gitRepoTargetPath);
        }
    }
    
    [ActionDefine("Git.Ignore")]
    public static void IgnoreWithPattern(string ignorePattern)
    {
        var ignorePath = Path.Combine(CmdParser.Get<ICommonCommandGroup>().ProjectRoot, ".gitignore");
        if (!File.Exists(ignorePath))
        {
            File.WriteAllText(ignorePath, ignorePattern);
        }
        else
        {
            var ignorePatterns = File.ReadAllLines(ignorePath).ToList().ToHashSet();
            if (ignorePatterns.All(pattern => pattern.Trim() != ignorePattern.Trim()))
            {
                ignorePatterns.Add(ignorePattern);
            }
            File.WriteAllLines(ignorePath, ignorePatterns);
        }
    }

    [ActionDefine("Git.Pull")]
    public static void Pull(string targetPath)
    {
        SimpleExec.Command.Run("git", "reset --hard", targetPath);
        SimpleExec.Command.Run("git", "pull", targetPath);
    }
}