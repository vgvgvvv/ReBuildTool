using NiceIO;
using ReBuildTool.Common;
using ReBuildTool.Internal;
using ResetCore.Common;
using ResetCore.Common.Parser.Ini;

namespace ReBuildTool.Actions;

public class ReMake
{
    public static NPath ReMakeDir => GlobalPaths.IntermediaPath.Combine("ReMake").EnsureDirectoryExists();


    private static readonly string RootMakeListsContent = 
        @"
cmake_minimum_required(VERSION 3.17)
project(${projectName})
if(NOT REMAKE_ROOT_PATH)
    set(ReMakeDir ${CMAKE_CURRENT_SOURCE_DIR}/Intermedia/ReMake)
    include(${remakeInitPath})
    ReMake_InitProject()

    ReMake_AddSubDirsRec(""${ReMakeDir}"")
    ReMake_AddSubDirsRec(""Test"")
endif()

ReMake_AddSubDirsRec(""Src"")
        ";
    
    [ActionDefine("ReMake.Init")]
    public static void InitReMakeRoot(string projectName, ModuleMode mode = ModuleMode.Shared)
    {
        var recmakeName = "re-cmake";
        Git.GetFromGit("https://github.com/vgvgvvv/re-cmake", recmakeName, ReMakeDir);
        Git.IgnoreWithPattern("Intermedia");
        Git.IgnoreWithPattern("binary");
        Git.IgnoreWithPattern("temp");
        Git.IgnoreWithPattern("*_API.h");
        Git.IgnoreWithPattern("*.Target.json");
        
        var srcDir = GlobalPaths.ProjectRoot.Combine("Src").EnsureDirectoryExists();
        var testDir = GlobalPaths.ProjectRoot.Combine("Test").EnsureDirectoryExists();
        
        var rootMakeListsPath = GlobalPaths.ProjectRoot.Combine("CMakeLists.txt");
        if (!rootMakeListsPath.Exists())
        {
            ContextArgs.Context context = new ContextArgs.Context();
            context.AddArg("projectName", projectName);
            context.AddArg("remakeInitPath", $"${{ReMakeDir}}/{recmakeName}/Init.cmake");
            ContextArgs contextArgs = new ContextArgs(RootMakeListsContent);
            rootMakeListsPath.WriteAllText(contextArgs.GetText(context));
        }
        
        var mainDir = srcDir.Combine(projectName);
        MakeReMakeModule(projectName, mainDir, mode);
        
        MakeReMakeModule($"{projectName}_Test", testDir, ModuleMode.Exe);
        
    }

    [ActionDefine("ReMake.FromGit")]
    public static void InstallReMakeLibrary(string url, string name)
    {
        Git.GetFromGit(url, name, ReMakeDir);
    }

    public enum ModuleMode
    {
        Static,
        Shared,
        Exe
    }

    [ActionDefine("ReMake.MakeModule")]
    public static void MakeReMakeModule(string targetName, string targetPath, ModuleMode mode)
    {
        var rootPath = targetPath.ToNPath().EnsureDirectoryExists();
        var publicPath = rootPath.Combine("Public").EnsureDirectoryExists();
        var privatePath = rootPath.Combine("Private").EnsureDirectoryExists();
        ContextArgs.Context context = new ContextArgs.Context();
        context.AddArg("targetName", targetName);
        context.AddArg("targetFolderName", rootPath.FileName);
        context.AddArg("mode", mode.ToString().ToUpper());
        
        var moduleCMakeListPath = targetPath.ToNPath().Combine("CMakeLists.txt");
        if (!moduleCMakeListPath.Exists())
        {
            moduleCMakeListPath.CreateFile().WriteAllText( new ContextArgs(@"
set(TargetName ${targetName})
ReMake_AddTarget(
    TARGET_NAME $\{TargetName\}
    MODE SHARED
    INC ""${targetFolderName}/Public""
)
").GetText(context)
            );
            privatePath.Combine($"{targetName}.cpp")
                .CreateFile()
                .WriteAllText(new ContextArgs(@"
#include ""${targetName}.h""

").GetText(context)
                );
            publicPath.Combine($"{targetName}.h")
                .CreateFile()
                .WriteAllText(new ContextArgs(@"#pragma once

class ${targetName}
{

};

").GetText(context)
                    );
        }
        
        var moduleIniPath = targetPath.ToNPath().Combine($"{targetName}.module.ini");
        if (!moduleIniPath.Exists())
        {
            moduleIniPath.CreateFile().WriteAllText(new ContextArgs(@"# Define dependencies
[Module]
# +Dependencies=XXX

# Create init steps
[Init]
# +DependOn=""Action:XXX""
# +Action=(Name=XXX, Args=(XXX=XXX, XXX=XXX))

# Create build steps
[Build]
# +DependOn=""Action:XXX""
# +Action=(Name=XXX, Args=(XXX=XXX, XXX=XXX))

# Create a new action
# [Action:XXX]

").GetText(context)
            );
        }
    }
    
}