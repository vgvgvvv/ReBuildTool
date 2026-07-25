using System.Collections;
using NiceIO;
using ResetCore.Common;

namespace ReBuildTool.ToolChain;

public partial class GccToolChain 
{
    internal override CppCompileInvocation MakeCompileInvocation(CppCompilationUnit compileUnit)
    {
        var invocation = new CppCompileInvocation(compileUnit);
        invocation.ProgramName = CompilerExecutableFor(compileUnit.SourceFile);
        invocation.EnvVars.AddRange(EnvVars());
        invocation.Arguments.AddRange(CompileArgsFor(compileUnit));
        return invocation;
    }

    public override IEnumerable<string> CompileArgsFor(CppCompilationUnit compileUnit)
    {
        if (compileUnit.SourceFile.ExtensionWithDot == ".asm")
        {
            foreach (var arg in CompileArgsForAssembly(compileUnit))
            {
                yield return arg;
            }
        }
        else
        {
            foreach (var arg in CompileArgsForCpp(compileUnit))
            {
                yield return arg;
            }
        }
    }

    private IEnumerable<string> CompileArgsForAssembly(CppCompilationUnit compileUnit)
    {
        yield return "-f";
        if (Arch == new x64Architecture())
        {
            yield return "-elf64";
        }

        yield return "-o";
        yield return compileUnit.OutputFile.InQuotes();
        
        yield return compileUnit.SourceFile.InQuotes();
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

        // Dependency-file emission for the Ninja backend (deps = gcc). -MMD produces a
        // .d file covering user includes only (no system headers — keeps it lean);
        // -MF pins the output path next to the object file. Only set on the Ninja path.
        if (compileUnit.DependencyFilePath != null)
        {
            yield return "-MMD";
            yield return "-MF";
            yield return compileUnit.DependencyFilePath.InQuotes();
        }

        yield return "-o";
        yield return compileUnit.OutputFile.InQuotes();

        yield return compileUnit.SourceFile.InQuotes();

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
                yield return "-Os";
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

    }

    public override IEnumerable<string> ToolChainDefines()
    {
        yield return "COMPILER_GCC";
    }

    public override bool CanBeCompiled(NPath sourceFile)
    {
        var extension = sourceFile.ExtensionWithDot;
        return extension == ".c" || 
               extension == ".cpp" || 
               extension == ".cc" || 
               extension == ".cxx" ||
               extension == ".asm" ||
               extension == ".inl";
    }

    public override NPath CompilerExecutableFor(NPath sourceFile)
    {
        return LinuxSdk.GetCompiler(sourceFile);
    }
}