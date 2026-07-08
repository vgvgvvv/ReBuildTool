using NiceIO;

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
		// All target/dependency paths in the generated Makefile are absolute, so the working
		// directory doesn't affect correctness, but nmake/make still need a stable one to run in.
		var shell = Shell.Create().WithProgram(exe).WithWorkspace(makeFile.Parent);

		if (PlatformHelper.IsWindows())
		{
			// /F is required here - without it, nmake treats the makefile path as a target name
			// instead of "the file to read", and since that path happens to exist on disk (it's
			// the makefile itself), nmake considers it already satisfied and silently does nothing.
			shell.WithArguments(new List<string>() {
					"/F", makeFile.InQuotes(),
				});
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
