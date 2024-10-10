﻿using NiceIO;

using ReBuildTool.Service.Global;

using ResetCore.Common;

namespace ReBuildTool.ToolChain.SDK.MakeFile;

public class MakeFile
{
	public static NPath GetMakeFileExecutable(BuildOptions buildOptions)
	{
		if (PlatformHelper.IsWindows())
		{
			var msvcSdk = MsvcSDK.FindLatestSDK()
				.SetArch(buildOptions.Architecture)
				.UseLastestVCPaths()
				.UseLatestWindowsKit();
			var makeFile = msvcSdk.CurrentVCPaths.GetBinPath(new x64Architecture());
			return makeFile.Combine("nmake.exe");
		}
		else
		{
			return new NPath("/usr/bin/make");
		}
	}

	public static bool RunMakeFile(BuildOptions buildOptions, NPath makeFile)
	{
		var exe = GetMakeFileExecutable(buildOptions);
		if (!exe.Exists())
		{
			Log.Error("cannot find makefile executable");
			return false;
		}
		var jCount = Math.Min(Environment.ProcessorCount * 2, 16);
		var shell = Shell.Create()
			.WithProgram(exe)
			.WithArguments(new List<string>() {
				makeFile.InQuotes(),
			});

		if (PlatformHelper.IsWindows())
		{
			shell.AppendArgument("/NOLOGO");
		}
		else
		{
			shell.AppendArgument($"-j{jCount}");
		}
		
		shell.Execute()
			.WaitForEnd();
		if(shell.Process.ExitCode != 0)
		{
			Log.Error($"makefile {makeFile} failed");
			return false;
		}
		return true;
	}
	
}
