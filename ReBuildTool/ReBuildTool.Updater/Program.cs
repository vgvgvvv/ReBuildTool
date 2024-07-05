using System.Reflection;
using NiceIO;
using ReBuildTool.Actions;
using ReBuildTool.Service.Global;

var rbtToolHome = GlobalPaths.ReBuildToolHome;
var rebuildToolGitRoot = rbtToolHome.Combine("ReBuildTool");
if (!rbtToolHome.Exists())
{
	Git.GetFromGit("git@github.com:vgvgvvv/ReBuildTool.git", "ReBuildTool", rbtToolHome);
}
else
{
	Git.Pull(rebuildToolGitRoot);
}

var isRunningBuildedUpdater = Assembly.GetAssembly(typeof(Program)).Location.ToNPath().IsChildOf(rebuildToolGitRoot);

if (PlatformHelper.IsWindows())
{
	Cmd.RunCmd(rebuildToolGitRoot.Combine("BuildScript/BuildAll.bat"), "", rebuildToolGitRoot);
	if (!isRunningBuildedUpdater)
	{
		Cmd.RunCmd(rebuildToolGitRoot.Combine("BuildScript/BuildUpdater.bat"), "", rebuildToolGitRoot);
	}
}
else
{
	var buildAllSh = rebuildToolGitRoot.Combine("BuildScript/BuildAll.sh");
	var buildUpdaterSh = rebuildToolGitRoot.Combine("BuildScript/BuildUpdater.sh");
	Cmd.MakeExecutable(buildAllSh);
	Cmd.MakeExecutable(buildUpdaterSh);
	Cmd.RunCmd(buildAllSh, "", rebuildToolGitRoot);
	if (!isRunningBuildedUpdater)
	{
		Cmd.RunCmd(buildUpdaterSh, "", rebuildToolGitRoot);
	}
}
