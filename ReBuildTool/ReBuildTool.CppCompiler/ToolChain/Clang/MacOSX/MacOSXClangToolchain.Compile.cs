using System.Collections;
using NiceIO;
using ReBuildTool.CppCompiler;
using ReBuildTool.Service.CompileService;

namespace ReBuildTool.ToolChain;

public partial class MacOSXClangToolchain
{
    public override IEnumerable<string> CompileArgsFor(CppCompilationUnit compileUnit)
    {
        if (IsObjectiveC(compileUnit.SourceFile))
        {
            foreach (var arg in CompileArgsForObjectiveC(compileUnit))
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
    
    private IEnumerable<string> CompileArgsForCpp(CppCompilationUnit compileUnit)
    {
        yield return "-c";

        foreach (var targetPlatformArg in TargetPlatformArgs())
        {
            yield return targetPlatformArg;
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
            yield return $"-I{includePath}";
        }

        if (Configuration == BuildConfiguration.Debug)
        {
            yield return "-g3";
        }

        // Dependency-file emission for the Ninja backend (deps = gcc). -MMD = user
        // includes only; -MF pins the output next to the object file. Also applies to
        // ObjC/ObjC++ sources since CompileArgsForObjectiveC delegates here.
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

    private IEnumerable<string> CompileArgsForObjectiveC(CppCompilationUnit compilationUnit)
    {
        yield return "-fobjc-arc";
        
        if (compilationUnit.OwnerModule is IObjectiveCModule ocModule)
        {
            if (ocModule.Frameworks.Count > 0)
            {
                foreach (var framework in ocModule.Frameworks)
                {
                    yield return "-framework";
                    yield return framework;
                }
            }
        }
        
        foreach (var arg in CompileArgsForCpp(compilationUnit))
        {
            yield return arg;
        }
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

        // Emit the builder flags for all source kinds (C, C++, ObjC, ObjC++).
        // GetAllArguments(isCSource) already skips C++-only flags for plain C,
        // and ObjC/ObjC++ need the C++ flags (std/RTTI/exceptions) just like C++.
        foreach (var argument in unit.CompileArgsBuilder.GetAllArguments(unit.IsCFile))
        {
            yield return argument;
        }

        if (!unit.IsCFile)
        {
            yield return "-stdlib=libc++";
        }
    }
    
    public virtual IEnumerable<string> TargetPlatformArgs()
    {
        yield return "-target";
        string archName;
        if (Arch is x64Architecture)
        {
            archName = "x86_64";
        }
        else if (Arch is ARM64Architecture)
        {
            archName = "arm64";
        }
        else
        {
            throw new Exception("Unsupported architecture");
        }
        
        var targetVersion = MacOSXCompileArgs.Get().MacOSXTargetVersion;
        yield return $"{archName}-apple-macosx{targetVersion.Value}";
    }

    public override IEnumerable<string> ToolChainDefines()
    {
        foreach (string toolChainDefine in base.ToolChainDefines())
        {
            yield return toolChainDefine;
        }
    }

    public override bool CanBeCompiled(NPath sourceFile)
    {
        return base.CanBeCompiled(sourceFile) || IsObjectiveC(sourceFile);
    }

    private bool IsObjectiveC(NPath sourceFile)
    {
        var extension = sourceFile.ExtensionWithDot;
        return extension == ".mm" || extension == ".m";
    }
}