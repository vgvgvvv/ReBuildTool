using Bullseye;
using ReBuildTool.Internal;
using ResetCore.Common;

Log.Info("Begin Generate..");

CmdParser.Parse();

var command = CommonCommandGroup.Get();

var project = ModuleProject.Create(command.Target)
    .Parse(command.ProjectRoot);

Targets root = new Targets();
var targets = new List<string>();

switch (command.Mode)
{
    case RunMode.Init:
        project.SetupInitTargets(root, ref targets);
        break;
    case RunMode.Build:
        project.SetupBuildTargets(root, ref targets);
        break;
    default:
        break;
}

await root.RunAndExitAsync(new[] { $"{command.Target} : {command.Mode}" }, 
    ex => ex is SimpleExec.ExitCodeException);

Log.Info("Finished..");