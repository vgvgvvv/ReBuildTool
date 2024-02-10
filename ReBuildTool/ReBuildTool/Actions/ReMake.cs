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
    public static void InitReMakeRoot(string projectName)
    {
        var recmakeName = "re-cmake";
        Git.GetFromGit("https://github.com/vgvgvvv/re-cmake", recmakeName, ReMakeDir);
        Git.IgnoreWithPattern("Intermedia");
        
        GlobalPaths.ProjectRoot.Combine("Src").EnsureDirectoryExists();
        GlobalPaths.ProjectRoot.Combine("Test").EnsureDirectoryExists();
        
        var rootMakeListsPath = GlobalPaths.ProjectRoot.Combine("CMakeLists.txt");
        if (!rootMakeListsPath.Exists())
        {
            ContextArgs.Context context = new ContextArgs.Context();
            context.AddArg("projectName", projectName);
            context.AddArg("remakeInitPath", $"${{ReMakeDir}}/{recmakeName}/Init.cmake");
            ContextArgs contextArgs = new ContextArgs(RootMakeListsContent);
            rootMakeListsPath.WriteAllText(contextArgs.GetText(context));
        }
        
    }

    [ActionDefine("ReMake.FromGit")]
    public static void InstallReMakeLibrary(string url, string name)
    {
        Git.GetFromGit(url, name, ReMakeDir);
    }
    
}