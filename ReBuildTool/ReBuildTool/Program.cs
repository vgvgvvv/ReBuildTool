using ReBuildTool.Common;
using ReBuildTool.Internal;

Log.Info("Main", "Begin Generate..");

CmdParser.Parse();
ModuleParser.Parse(CommonCommandGroup.Get().ProjectRoot);

Log.Info("Main", "Finished..");