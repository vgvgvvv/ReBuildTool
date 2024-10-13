using ReBuildTool.Actions;
using ReBuildTool.Service.Global;

namespace ReBuildTool.Service.CompileService.HeaderTool;

public partial class HeaderToolPluginSupport
{
    private void BuildHeaderTool()
    {
        bool needBuild = false;
        var headerToolArgs = HeaderToolArgs.Get();
        var installPath = HeaderToolRoot.Combine("ResetHeaderTool");
        if (installPath.Combine(".git").Exists())
        {
            if (headerToolArgs.NeedBuildHeaderTool)
            {
                Git.Pull(installPath);
                needBuild = true;
            }
        }
        else
        {
            HeaderToolRoot.EnsureDirectoryExists();
            Git.GetFromGit("git@github.com:vgvgvvv/ResetHeaderTool.git", "ResetHeaderTool", HeaderToolRoot);
            needBuild = true;
        }

        if (!needBuild)
        {
            return;
        }

        var buildScript = installPath.Combine("Scripts/BuildAll.bat");
        if (!PlatformHelper.IsWindows())
        {
            buildScript.ChangeExtension(".sh");
        }
        Cmd.RunCmd(buildScript, "", HeaderToolRoot);
    }
}