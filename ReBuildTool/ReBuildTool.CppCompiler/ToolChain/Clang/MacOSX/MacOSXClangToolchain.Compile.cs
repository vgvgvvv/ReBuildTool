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
        
    }

    public override IEnumerable<string> ToolChainDefines()
    {
        yield break;
    }
    
}