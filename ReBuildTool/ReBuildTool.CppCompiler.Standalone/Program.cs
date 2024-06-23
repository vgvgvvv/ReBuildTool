using System.Reflection;
using ReBuildTool.CppCompiler.Standalone;
using ReBuildTool.ToolChain;
using ResetCore.Common;




CmdParser.Parse();

var project = CppBuildProject.Create()
	.Parse(CppCompilerArgs.Get().ProjectRoot);

project.Setup();

project.Build();



