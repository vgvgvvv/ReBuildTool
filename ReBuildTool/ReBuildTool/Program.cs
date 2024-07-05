using Bullseye;
using ReBuildTool.Internal;
using ReBuildTool.Service.CommandGroup;
using ReBuildTool.Service.CompileService;
using ReBuildTool.Service.Context;
using ResetCore.Common;

Log.Info("Begin Generate..");
Log.Info(Environment.CommandLine);

CmdParser.Parse<Program>();
var command = CommonCommandGroup.Get();
BoosterSupport.SetupBooster(command.BoosterSource);

var moduleProject = ModuleProject.Create(command.Target)
    .Parse(command.ProjectRoot);
ICppProject CppProject = 
    ServiceContext.Instance.Create<ICppProject>(command.ProjectRoot).Value;
CppProject.Parse();

Targets root = new Targets();
var targets = new List<string>();

switch (command.Mode.Value)
{
    case RunMode.Init:
        moduleProject.SetupInitTargets(root, ref targets);
        CppProject.Setup();
        break;
    case RunMode.Build:
        moduleProject.SetupBuildTargets(root, ref targets);
        CppProject.Build(command.Target);
        break;
    case RunMode.Clean:
        CppProject.Clean();
        break;
    case RunMode.ReBuild:
        CppProject.ReBuild(command.Target);
        break;
    default:
        break;
}
await root.RunAndExitAsync(targets, 
    ex => ex is SimpleExec.ExitCodeException);

Log.Info("Finished..");