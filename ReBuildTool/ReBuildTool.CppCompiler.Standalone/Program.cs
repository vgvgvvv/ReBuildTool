﻿using System.Reflection;
using NiceIO;
using ReBuildTool.CppCompiler.Standalone;
using ReBuildTool.Service.CompileService;
using ReBuildTool.Service.Context;
using ResetCore.Common;


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



