using ReBuildTool.Common;
using ReBuildTool.Internal;
using ResetCore.Common;
using ResetCore.Common.Parser.Ini;

namespace ReBuildTool.Actions;

public class Git
{
    [ActionDefine("Git.Get")]
    public static void GetFromGit(string url, 
        [ActionParameter("WorkDirectory")] string targetPath, 
        string tag)
    {
        if (!Directory.Exists(targetPath))
        {
            var args = new List<string>();
            args.Add("clone");
            args.Add(url);
            if (!string.IsNullOrEmpty(targetPath))
            {
                args.Add(targetPath);
            }
            if (!string.IsNullOrEmpty(tag))
            {
                args.Add("--branch");
                args.Add(tag);
            }
            Log.Info("Run Git: ", "git", args.Join(" "));
            SimpleExec.Command.Run("git" , args.Join(" "));
        }
    }
}