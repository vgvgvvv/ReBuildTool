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
            yield return $"-I\"{includePath}\"";
        }

        if (Configuration == BuildConfiguration.Debug)
        {
            yield return "-g3";
        }

        yield return "-o";
        yield return compileUnit.OutputFile.InQuotes();
        
        yield return compileUnit.SourceFile.InQuotes();
    }
    
    private IEnumerable<string> DefaultCompileFlags(CppCompilationUnit unit)
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

        yield return $"-D__ANDROID_API__={NdkClangSdk.Setting.Version}";
        
        foreach (var argument in unit.CompileArgsBuilder.GetAllArguments())
        {
            yield return argument;
        }

        yield return "-target";
        yield return NdkClangSdk.Setting.TargetPlatformName;

        yield return "--sysroot=" + NdkClangSdk.SysRoot.InQuotes();
            
        yield return "-stdlib=libc++";
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