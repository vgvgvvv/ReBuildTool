using NiceIO;
using ResetCore.Common;

namespace ReBuildTool.Internal;

public class BoosterSupport
{
	public static void SetupBooster(string boosterPath)
	{
		if (string.IsNullOrEmpty(boosterPath) || !File.Exists(boosterPath))
		{
			return;
		}

		var ex = Path.GetExtension(boosterPath);
		if (ex != ".sh" && ex != ".bat")
		{
			Log.Exception($"not supported booster file type {ex} ..");
		}
		
		var backup = boosterPath.ToNPath().Parent.Combine($"Booster-Backup{ex}");
		boosterPath.ToNPath().Copy(backup);
		Log.Info("copy ");
		GlobalPaths.ScriptRoot.Combine($"RBTBooster{ex}").Copy(boosterPath.ToNPath());
	}
}