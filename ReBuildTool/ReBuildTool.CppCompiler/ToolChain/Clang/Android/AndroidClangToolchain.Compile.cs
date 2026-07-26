namespace ReBuildTool.ToolChain.Android;

public partial class AndroidClangToolchain
{
    public override IEnumerable<string> CompileArgsFor(CppCompilationUnit compileUnit)
    {
        foreach (var arg in CompileArgsForCpp(compileUnit))
        {
            yield return arg;
        }
    }

    private IEnumerable<string> CompileArgsForCpp(CppCompilationUnit compileUnit)
    {
        yield return "-c";
        
        foreach (var compileFlag in compileUnit.CompileFlags.Concat(DefaultCompileFlags(compileUnit)))
        {
            yield return compileFlag;
        }
			
        foreach (var define in compileUnit.Defines.Concat(ToolChainDefines()))
        {
            yield return $"-D{define}";
        }
			
        foreach (var includePath in compileUnit.IncludePaths.Concat(ToolChainIncludePaths()))
        {
            yield return $"-I{includePath}";
        }

        if (Configuration == BuildConfiguration.Debug)
        {
            yield return "-g3";
        }

        yield return "-o";
        yield return compileUnit.OutputFile.ToString();

        yield return compileUnit.SourceFile.ToString();
    }
    
    private IEnumerable<string> DefaultCompileFlags(CppCompilationUnit unit)
    {
        // Optimization: emit the config-driven default only when the module
        // hasn't overridden it via builder.SetOptimizationLevel.
        if (unit.CompileArgsBuilder.OptimizationLevel == null)
        {
            if (Configuration == BuildConfiguration.Debug)
            {
                yield return "-O0";
            }

            if (Configuration == BuildConfiguration.Release ||
                Configuration == BuildConfiguration.ReleasePlus )
            {
                yield return "-O3";
            }

            if (Configuration == BuildConfiguration.ReleaseSize)
            {
                yield return "-Oz";
            }
        }

        // Emit per-function/per-data sections in non-Debug builds so the linker
        // can garbage-collect unreferenced ones (-Wl,--gc-sections in Link.cs).
        if (Configuration != BuildConfiguration.Debug)
        {
            yield return "-ffunction-sections";
            yield return "-fdata-sections";
        }

        yield return $"-D__ANDROID_API__={NdkClangSdk.Setting.Version}";

        foreach (var argument in unit.CompileArgsBuilder.GetAllArguments(unit.IsCFile))
        {
            yield return argument;
        }

        // PIC: Android always needs position-independent code. If the module
        // explicitly set EnablePIC=false, honor that; otherwise default to -fPIC.
        if (unit.CompileArgsBuilder.EnablePIC == null)
        {
            yield return "-fPIC";
        }

        yield return "-target";
        yield return NdkClangSdk.Setting.TargetPlatformName;

        yield return "--sysroot=" + NdkClangSdk.SysRoot;

        // is
        // yield return "-stdlib=libc++";
    }

    public override IEnumerable<string> ToolChainDefines()
    {
        foreach (string toolChainDefine in base.ToolChainDefines())
        {
            yield return toolChainDefine;
        }
        
        yield return "LINUX";
        yield return "ANDROID";
        yield return "PLATFORM_ANDROID";
        yield return "__linux__";
        yield return "__STDC_FORMAT_MACROS";
        if (Arch is ARM64Architecture)
            yield return "TARGET_ARM64";
    }
}