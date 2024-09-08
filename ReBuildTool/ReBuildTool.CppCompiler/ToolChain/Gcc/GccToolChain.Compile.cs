using System.Collections;
using NiceIO;
using ResetCore.Common;

namespace ReBuildTool.ToolChain;

public partial class GccToolChain 
{
    internal override CppCompileInvocation MakeCompileInvocation(CppCompilationUnit compileUnit)
    {
        var invocation = new CppCompileInvocation();
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

    public override bool CanBeCompiled(NPath sourceFile)
    {
        var extension = sourceFile.ExtensionWithDot;
        return extension == ".c" || 
               extension == ".cpp" || 
               extension == ".cc" || 
               extension == ".cxx" ||
               extension == ".asm";
    }

    public override NPath CompilerExecutableFor(NPath sourceFile)
    {
        return LinuxSdk.GetCompiler(sourceFile);
    }
}