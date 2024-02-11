using ReBuildTool.Common;
using ResetCore.Common;

namespace ReBuildTool.Actions;

public static class Cmd
{
	[ActionDefine("Cmd.Run")]
	public static void RunCmd(string cmd, string args, string workDir)
	{
		Log.Info("Run Cmd: ", cmd, " ", args, " in ", workDir);
		SimpleExec.Command.Run(cmd, args, workDir);
	}

	public static void MakeExecutable(string path)
	{
		if (OperatingSystem.IsMacOS() || OperatingSystem.IsLinux())
		{
			SimpleExec.Command.Run("/bin/bash", $"-c \"chmod +x {path}\"");
		}
	}
}