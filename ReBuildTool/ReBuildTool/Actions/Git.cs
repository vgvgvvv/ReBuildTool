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
        //SimpleExec.Command.Run();
    }
}