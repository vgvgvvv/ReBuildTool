// HeaderToolTest target — exercises the ResetHeaderTool reflection codegen
// end-to-end: a module with a RECLASS-annotated header, the
// ResetEngineClassExtension plugin, and the framework's auto-injection of the
// generated HeaderToolGen/ tree into the compile flow.
using System.Collections.Generic;
using ReBuildTool.Service.CompileService.HeaderTool;
using ReBuildTool.ToolChain;

public class HeaderToolTestTarget : CppTargetRule, IHeaderToolTarget
{
    public HeaderToolTestTarget()
    {
        UsedModules.Add("ReflectModule");
        // Drives ResetHeaderTool (projectType=ReBuildTool) with the reflection
        // codegen plugin to emit code under ReflectModule/HeaderToolGen/Extension/.
        Plugins.Add(new HeaderToolPluginSupport());
    }

    // IHeaderToolTarget: hand ResetHeaderTool the reflection-codegen plugin DLL
    // and entry class name. The DLL ships under the sample's Plugins/ dir.
    public List<string> PluginDlls => new()
    {
        "Plugins/ResetEngineClassExtension.dll"
    };

    public List<string> PluginNames => new()
    {
        "ResetEngineClassExtensionPlugin"
    };

    public List<string> ExtraArgs => new();
}
