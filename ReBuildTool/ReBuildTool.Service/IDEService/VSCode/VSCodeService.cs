using NiceIO;
using ReBuildTool.Service.CompileService;
using ReBuildTool.Service.Context;

namespace ReBuildTool.Service.IDEService.VSCode;

public interface IVSCodeGenerator : IProvideByService
{
    string Name { get; }

    string OutputPath { get; }

    // Generates a .vscode folder (tasks.json / launch.json / c_cpp_properties.json) under
    // <paramref name="output"/> so VS Code can build, run, debug and clean the project by
    // delegating every action to rbt itself.
    void GenerateVSCodeProject(ICppSourceProviderInterface source, NPath output);

    bool FlushAllFiles();
}
