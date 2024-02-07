using ReBuildTool.Internal;
using ResetCore.Common;

Log.Info("Begin Generate..");

CmdParser.Parse();
ModuleProject.Parse(CommonCommandGroup.Get().ProjectRoot);

var mode = CommonCommandGroup.Get().Mode;
switch (mode)
{
    case RunMode.Init:
        break;
    case RunMode.Build:
        break;
    default:
        break;
}

Log.Info("Finished..");