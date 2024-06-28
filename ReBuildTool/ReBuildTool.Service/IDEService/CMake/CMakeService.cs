﻿using NiceIO;
using ReBuildTool.Service.CompileService;
using ReBuildTool.Service.Context;

namespace ReBuildTool.Service.IDEService.CMake;

public interface ICMakeLists
{
    string name { get; }
    
    NPath fullPath { get; }

    bool FlushToFile();
}

public interface ICMakeGenerator : IProvideByService
{
    string Name { get; }

    string OutputPath { get; }

    ICMakeLists GenerateCMakeProject(ICppSourceProviderInterface source, NPath output);
    
    bool FlushAllCMakeFile();
}