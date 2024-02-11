using NiceIO;
using ResetCore.Common;

namespace ReBuildTool.Internal;

public class BoosterSupport
{
	public static void SetupBooster(string boosterPath)
	{
		if (string.IsNullOrEmpty(boosterPath) || !File.Exists(boosterPath))
		{
			Log.Info("No booster file found..");
			return;
		}

		var ex = Path.GetExtension(boosterPath);
		if (ex != ".sh" && ex != ".bat")
		{
			Log.Exception($"not supported booster file type {ex} ..");
		}
		
		var targetPath = GlobalPaths.ScriptRoot.Combine($"RBTBooster{ex}");
		Log.Info($"copy {targetPath} to {boosterPath} ..");
		var sourcePath = GlobalPaths.ScriptRoot.Combine($"RBTBooster{ex}");
		if (sourcePath.Exists())
		{
			sourcePath.Copy(boosterPath.ToNPath());
			if (OperatingSystem.IsMacOS() || OperatingSystem.IsLinux())
			{
				SimpleExec.Command.Run("/bin/bash", $"-c \"chmod +x {boosterPath}\"");
			}
		}
		else
		{
			Log.Error($"cannot found source booster file in {sourcePath} ..");
		}
		
	}
}