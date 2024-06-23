using System.Reflection;
using NiceIO;
using ReBuildTool.ToolChain;
using ResetCore.Common;
using UnityCompiler.Internal;

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
    public void TestBaseProject()
    {
        CmdParser.Parse<Tests>();
        
        var project = CppBuildProject
            .Create(BuildCppFolder)
            .Parse();
        
        project.Setup();
        project.Build();
        
    }
}