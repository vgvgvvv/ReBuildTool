using NiceIO;
using ReBuildTool.ToolChain;
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
    public void TestBaseProject()
    {
        CmdParser.Parse();
        
        var project = CppBuildProject
            .Create(BuildCppFolder)
            .Parse();
        
        // project.Setup();
        // project.Build();
        
    }
}