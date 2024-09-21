using System.Reflection;
using System.Runtime.InteropServices;
using NiceIO;
using ReBuildTool.Service.CompileService;
using ReBuildTool.Service.Context;
using ReBuildTool.ToolChain;
using ReBuildTool.ToolChain.Project;
using ResetCore.Common;

namespace ReBuildTool.Test;

public class Tests
{

    private NPath BuildCppFolder;
        
    [SetUp]
    public void Setup()
    {
        
        BuildCppFolder = TestCaseGlobalVars.SampleDirectory.Combine("BuildCpp");
    }

    [Test]
    public void AllReferencedAssemblies()
    {
        var entryAssembly = Assembly.GetAssembly(typeof(Tests));
        Log.Info(entryAssembly.FullName);
        foreach (var assemblyPath in entryAssembly.Location.ToNPath().Parent.Files("*.dll", true))
        {
            if (!MonoUtil.IsDotNetAssembly(assemblyPath))
            {
                continue;
            }
            var assembly = Assembly.LoadFrom(assemblyPath);
            Log.Info(assembly.FullName);
        }
    }

    [Test]
    public void GetRuntimePath()
    {
        string frameworkPath = RuntimeEnvironment.GetRuntimeDirectory();
        Log.Info(frameworkPath);
    }

    [Test]
    public void TestBaseProject()
    {
        CmdParser.Parse<Tests>();
        ServiceContext.Instance.Init();
        //CppCompilerArgs.Get().DryRun = true;
        
        var path = BuildCppFolder;
        var project = ServiceContext.Instance.Create<ICppProject>(path).Value;
        project.Parse();
        project.Setup();
        project.Build();
        
    }

    [Test]
    public void TestProjectGenerate()
    {
        CmdParser.Parse<Tests>();
        ServiceContext.Instance.Init();
        //CppCompilerArgs.Get().DryRun = true;
        
        var path = BuildCppFolder;
        var project = ServiceContext.Instance.Create<ICppProject>(path).Value;
        project.Parse();
        project.Setup();
    }
}