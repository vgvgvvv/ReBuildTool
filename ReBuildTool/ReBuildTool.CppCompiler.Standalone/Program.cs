﻿using NiceIO;
using ReBuildTool.CppCompiler;
using ReBuildTool.Service.CompileService;
using ReBuildTool.Service.Context;
using ResetCore.Common;


Log.Info("Starting..");
try
{
	if (!CmdParser.Parse<Program>())
	{
		return;
	}
	ServiceContext.Instance.Init();

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
}



