using Bullseye;

using ReBuildTool.Internal;
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

    var logFile = GlobalPaths.IntermediaPath.Combine("Logs", "Build.log");
    logFile.EnsureParentDirectoryExists();
    logFile.DeleteIfExists();
    Log.AppendLogger(new FileLogger(logFile).WithDate());
    // Wrap the now-complete logger (Console + File) in a single background-writer
    // queue: the Parallel.ForEach compile loop in CppBuilder calls Log.* from
    // worker threads, and the underlying Console/FileLogger sinks are not
    // thread-safe. The queue gives whole-line atomicity + strict FIFO ordering
    // with one writer thread. Flushed by Log.Shutdown() in the finally block.
    Log.EnableAsync();

    var root = GlobalPaths.ProjectRoot;
    var cppProject = ServiceContext.Instance.Create<ICppProject>(root);
    var projects = new List<IProjectInterface>
    {
        cppProject.Value
    };
    var targetName = command.Target.Value;
    if (string.IsNullOrEmpty(targetName))
    {
        targetName = root.FileName;
    }

    foreach (var project in projects)
    {
        // Restore does not go through Parse(): Parse() also compiles and loads the rule assembly,
        // and scaffolds a default Target/Module when the project has none. Neither belongs in
        // "fetch the packages and write the lock, then stop".
        if (command.Mode.Value == RunMode.Restore)
        {
            project.Restore();
            continue;
        }

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
    // Flush the async queue so the console and Build.log are fully written before
    // the process exits. No-op when async logging was not enabled.
    Log.Shutdown();
}
