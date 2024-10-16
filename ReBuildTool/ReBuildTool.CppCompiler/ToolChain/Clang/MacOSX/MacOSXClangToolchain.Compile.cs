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

    private IEnumerable<string> CompileArgsForObjectiveC(CppCompilationUnit compilationUnit)
    {
        yield return "-fobjc-arc";
        
        foreach (var targetPlatformArg in TargetPlatformArgs())
        {
            yield return targetPlatformArg;
        }
        
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
        
        if (!IsObjectiveC(unit.SourceFile))
        {
            foreach (var argument in unit.CompileArgsBuilder.GetAllArguments())
            {
                yield return argument;
            }
            
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