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
		GlobalPaths.ScriptRoot.Combine($"RBTBooster{ex}").Copy(boosterPath.ToNPath());
	}
}