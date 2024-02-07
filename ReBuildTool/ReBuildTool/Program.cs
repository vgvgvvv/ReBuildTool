using ReBuildTool.Common;
using ReBuildTool.Internal;

Log.Info("Main", "Begin Generate..");

CmdParser.Parse();
ModuleParser.Parse(CommonCommandGroup.Get().ProjectRoot);

var mode = CommonCommandGroup.Get().Mode;
switch (mode)
{
    case RunMode.Init:
        ModuleParser.InitModules();
        break;
    case RunMode.Build:
        ModuleParser.BuildModules();
        break;
    default:
        throw new ArgumentOutOfRangeException();
}


Log.Info("Main", "Finished..");