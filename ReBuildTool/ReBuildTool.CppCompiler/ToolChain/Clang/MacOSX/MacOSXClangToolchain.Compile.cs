namespace ReBuildTool.ToolChain;

public partial class MacOSXClangToolchain
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

        yield return "-arch";
        if (Arch is x64Architecture)
        {
            yield return "x86_64";
        }
        else if (Arch is ARM64Architecture)
        {
            yield return "arm64";
        }
        else
        {
            throw new NotSupportedException($"Unsupported architecture {Arch.Name}");
        }
        
        yield return "-isysroot";
        yield return XCodeSdk.PlatformSDK.SDKPath;
        
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
        
        foreach (var argument in unit.CompileArgsBuilder.GetAllArguments())
        {
            yield return argument;
        }
        
        yield return "-stdlib=libc++";
    }

    public override IEnumerable<string> ToolChainDefines()
    {
        yield break;
    }
    
}