using NiceIO;
using ReBuildTool.Actions;
using ReBuildTool.Service.CommandGroup;
using ReBuildTool.Service.Global;
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
			Cmd.MakeExecutable(boosterPath);
		}
		else
		{
			Log.Error($"cannot found source booster file in {sourcePath} ..");
		}

		{
			var initBat = GlobalPaths.ProjectRoot.Combine($"InitProject{ex}");
			if (!initBat.Exists())
			{
				initBat.CreateFile();
				ContextArgs.Context context = new ContextArgs.Context();
				context.AddArg("targetName", CommonCommandGroup.Get().Target);
				if (ex == ".sh")
				{
					initBat.WriteAllText(new ContextArgs(@"
cd $(dirname $0)
./RBTBooster.sh --init ${targetName}
").GetText(context)
					);
				}
				else if (ex == ".bat")
				{
					initBat.WriteAllText(new ContextArgs(@"
cd %~dp0
./RBTBooster.bat --init ${targetName}
").GetText(context)
					);
				}
				Cmd.MakeExecutable(initBat);
			}
		}

		{
			var buildBat = GlobalPaths.ProjectRoot.Combine($"BuildProject{ex}");
			if (!buildBat.Exists())
			{
				buildBat.CreateFile();
				ContextArgs.Context context = new ContextArgs.Context();
				context.AddArg("targetName", CmdParser.Get<CommonCommandGroup>().Target);
				if (ex == ".sh")
				{
					buildBat.WriteAllText(new ContextArgs(@"
cd $(dirname $0)
./RBTBooster.sh --build ${targetName}
").GetText(context)
					);
				}
				else if (ex == ".bat")
				{
					buildBat.WriteAllText(new ContextArgs(@"
cd %~dp0
./RBTBooster.bat --build ${targetName}
").GetText(context)
					);
				}
				Cmd.MakeExecutable(buildBat);
			}
		}
	}
}