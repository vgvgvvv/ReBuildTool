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
            // https rather than ssh: the repository is public, so an anonymous clone
            // works everywhere - including CI and any machine without a github key.
            Git.GetFromGit("https://github.com/vgvgvvv/ResetHeaderTool.git", "ResetHeaderTool", HeaderToolRoot);
            needBuild = true;
        }

        if (!needBuild)
        {
            return;
        }

        if (PlatformHelper.IsWindows())
        {
            // Not BuildAll.bat: that one ends with `pause` and would sit waiting for a
            // keypress. BuildAllNoPause.bat is the same script without it.
            Cmd.RunCmd(installPath.Combine("Scripts/BuildAllNoPause.bat"), "", HeaderToolRoot);
        }
        else
        {
            // ResetHeaderTool ships no BuildAll.sh; BuildHeaderTool.sh produces the same
            // Binary/<platform>/HeaderTool layout HeaderToolExePath looks in. Invoked
            // through bash because the script is not committed with the executable bit.
            Cmd.RunCmd("/bin/bash", installPath.Combine("Scripts/BuildHeaderTool.sh").InQuotes(), HeaderToolRoot);
        }
    }
}