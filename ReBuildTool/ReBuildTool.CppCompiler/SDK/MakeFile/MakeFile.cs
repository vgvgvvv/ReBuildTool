using NiceIO;

using ReBuildTool.Service.Global;

using ResetCore.Common;

namespace ReBuildTool.ToolChain.SDK.MakeFile;

public class MakeFile
{
	public static NPath GetMakeFileExecutable()
	{
		if (PlatformHelper.IsWindows())
		{
			var msvcSdk = MsvcSDK.FindLatestSDK()
				.SetArch(new x64Architecture())
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

	public static bool RunMakeFile(NPath makeFile)
	{
		var exe = GetMakeFileExecutable();
		if (!exe.Exists())
		{
			Log.Error("cannot find makefile executable");
			return false;
		}
		var shell = Shell.Create()
			.WithProgram(exe)
			.WithArguments(new List<string>() {
				makeFile.InQuotes()
			})
			.Execute()
			.WaitForEnd();
		if(shell.Process.ExitCode != 0)
		{
			Log.Error($"makefile {makeFile} failed");
			return false;
		}
		return true;
	}
	
}
