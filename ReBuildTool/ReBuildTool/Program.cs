using Bullseye;
using ReBuildTool.IniProject;
using ReBuildTool.Service.CommandGroup;
using ReBuildTool.Service.CompileService;
using ReBuildTool.Service.Context;
using ReBuildTool.Service.Global;
using ResetCore.Common;

Log.Info("Begin Generate..");
Log.Info(Environment.CommandLine);

try
{
    if (!CmdParser.Parse<Program>())
    {
        return;
    }
    var command = CmdParser.Get<ICommonCommandGroup>();
    BoosterSupport.SetupBooster();

    
    var root = GlobalPaths.ProjectRoot;
    var iniProject = ServiceContext.Instance.Create<IIniProject>(root);
    var cppProject = ServiceContext.Instance.Create<ICppProject>(root);
    var projects = new List<IProjectInterface>
    {
        iniProject.Value,
        cppProject.Value
    };
    var targetName = command.Target.Value;
    if (string.IsNullOrEmpty(targetName))
    {
        targetName = root.FileName;
    }

    foreach (var project in projects)
    {
        project.Parse();
        switch (command.Mode.Value)
        {
            case RunMode.Init:
                project.Setup();
                break;
            case RunMode.Build:
                project.Build(targetName);
                break;
            case RunMode.Clean:
                project.Clean();
                break;
            case RunMode.ReBuild:
                project.ReBuild(targetName);
                break;
            default:
                break;
        }
    }

}
catch (Exception e)
{
    Log.Error($"unhandled exception raised: {e}");
    Environment.ExitCode = 1;
}
finally
{
    Log.Info("Finished..");
}
