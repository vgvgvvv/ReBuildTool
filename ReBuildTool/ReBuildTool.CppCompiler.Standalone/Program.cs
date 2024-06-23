using System.Reflection;
using ReBuildTool.CppCompiler.Standalone;
using ReBuildTool.ToolChain;
using ResetCore.Common;




CmdParser.Parse<Program>();

var project = CppBuildProject.Create(CppCompilerArgs.Get().CppBuildRoot).Parse();

project.Setup();

project.Build();



