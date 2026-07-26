namespace ReBuildTool.ToolChain;

public partial class LinuxClangToolchain 
{
    public override IEnumerable<string> CompileArgsFor(CppCompilationUnit compileUnit)
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

        // Dependency-file emission for the Ninja backend (deps = gcc, same trait as
        // GCC). -MMD = user includes only; -MF pins the output next to the object.
        if (compileUnit.DependencyFilePath != null)
        {
            yield return "-MMD";
            yield return "-MF";
            yield return compileUnit.DependencyFilePath.ToString();
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

        foreach (var argument in unit.CompileArgsBuilder.GetAllArguments(unit.IsCFile))
        {
            yield return argument;
        }

        if (!unit.IsCFile)
        {
            yield return "-stdlib=libc++";
        }
    }
    
    public override IEnumerable<string> ToolChainDefines()
    {
        foreach (string toolChainDefine in base.ToolChainDefines())
        {
            yield return toolChainDefine;
        }
    }
}