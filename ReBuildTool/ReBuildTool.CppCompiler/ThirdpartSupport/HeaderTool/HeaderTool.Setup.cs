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

        // BuildHeaderTool, not BuildAll: the "build all" scripts also publish plugin
        // projects that no longer live in the ResetHeaderTool repository (and the .bat
        // ends with `pause`, which blocks an unattended build), while BuildAll.sh does
        // not exist there at all. BuildHeaderTool.{bat,sh} builds just the tool, into
        // the Binary/<platform>/HeaderTool layout HeaderToolExePath looks in.
        if (PlatformHelper.IsWindows())
        {
            Cmd.RunCmd(installPath.Combine("Scripts/BuildHeaderTool.bat"), "", HeaderToolRoot);
        }
        else
        {
            // Through bash: the script is not committed with the executable bit.
            Cmd.RunCmd("/bin/bash", installPath.Combine("Scripts/BuildHeaderTool.sh").InQuotes(), HeaderToolRoot);
        }
    }
}