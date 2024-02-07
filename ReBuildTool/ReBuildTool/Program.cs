using ReBuildTool.Internal;
using ResetCore.Common;

Log.Info("Main", "Begin Generate..");

CmdParser.Parse();
ModuleParser.Parse(CommonCommandGroup.Get().ProjectRoot);

Log.Info("Main", "Finished..");