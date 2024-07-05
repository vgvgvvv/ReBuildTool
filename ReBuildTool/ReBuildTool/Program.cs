using Bullseye;
using ReBuildTool.Internal;
using ReBuildTool.Service.Context;
using ResetCore.Common;

Log.Info("Begin Generate..");
Log.Info(Environment.CommandLine);

CmdParser.Parse<Program>();
var command = CommonCommandGroup.Get();
BoosterSupport.SetupBooster(command.BoosterSource);

var moduleProject = ModuleProject.Create(command.Target)
    .Parse(command.ProjectRoot);

Targets root = new Targets();
var targets = new List<string>();

switch (command.Mode.Value)
{
    case RunMode.Init:
        moduleProject.SetupInitTargets(root, ref targets);
        break;
    case RunMode.Build:
        moduleProject.SetupBuildTargets(root, ref targets);
        break;
    case RunMode.Clean:
        break;
    case RunMode.ReBuild:
        break;
    default:
        break;
}
await root.RunAndExitAsync(targets, 
    ex => ex is SimpleExec.ExitCodeException);

Log.Info("Finished..");