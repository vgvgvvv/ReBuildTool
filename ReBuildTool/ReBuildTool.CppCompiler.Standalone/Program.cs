using NiceIO;
using ReBuildTool.CppCompiler;
using ReBuildTool.Service.CompileService;
using ReBuildTool.Service.Context;
using ReBuildTool.Service.Global;
using ResetCore.Common;


Log.Info("Starting..");
try
{
	if (!CmdParser.Parse<Program>())
	{
		return;
	}
	ServiceContext.Instance.Init();

	var logFile = GlobalPaths.IntermediaPath.Combine("Logs", "Build.log");
	logFile.EnsureParentDirectoryExists();
	logFile.DeleteIfExists();
	Log.AppendLogger(new FileLogger(logFile).WithDate());
	// Wrap the now-complete logger (Console + File) in a single background-writer
	// queue so concurrent Log.* calls (parallel compile) are safe and FIFO-ordered.
	// Flushed by Log.Shutdown() in the finally block.
	Log.EnableAsync();

	var path = CppCompilerArgs.Get().ProjectRoot.Value.ToNPath();
	var project = ServiceContext.Instance.Create<ICppProject>(path).Value;
	project.Parse();
	project.Setup();
	project.Build();

}
catch (Exception e)
{
	Log.Error($"unhandled exception raised: {e}");
	Environment.ExitCode = 1;
}
finally
{
	Log.Info("Finished..");
	// Flush the async queue before exit. No-op when async logging was not enabled.
	Log.Shutdown();
}



