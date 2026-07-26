using NiceIO;

namespace ReBuildTool.ToolChain;

public partial class LinuxClangToolchain 
{
    protected override IEnumerable<string> LinkArgsFor(CppLinkUnit cppLinkUnit)
    {
        yield return $"-o";
        yield return cppLinkUnit.OutputPath.ToString();

        foreach (var defaultLinkFlag in DefaultLinkFlags(cppLinkUnit))
        {
            yield return defaultLinkFlag;
        }

        // Objects before libraries - see the same note in GccToolChain.Link: GNU ld
        // only takes archive members that resolve symbols it has already seen, so a -l
        // ahead of the objects is silently ignored.
        yield return "@" + cppLinkUnit.ResponseFile;

        foreach (var libraryArg in LibraryArgs(cppLinkUnit))
        {
            yield return libraryArg;
        }
    }

    private IEnumerable<string> LibraryArgs(CppLinkUnit cppLinkUnit)
    {
        foreach (var libraryPath in cppLinkUnit.LibraryPaths)
        {
            yield return "-L" + libraryPath;
        }

        foreach (var libpath in ToolChainLibraryPaths())
        {
            yield return "-L" + libpath;
        }

        foreach (var staticLibrary in ToolChainStaticLibraries())
        {
            yield return "-l" + staticLibrary.ToNPath();
        }

        foreach (var dynamicLibrary in ToolChainDynamicLibraries())
        {
            yield return "-l" + dynamicLibrary.ToNPath();
        }

        foreach (var staticLibrary in cppLinkUnit.StaticLibraries)
        {
            yield return "-l" + staticLibrary.ToNPath();
        }

        foreach (var dynamicLibrary in cppLinkUnit.DynamicLibraries)
        {
            yield return "-l" + dynamicLibrary.ToNPath();
        }
    }
    
    protected IEnumerable<string> DefaultLinkFlags(CppLinkUnit cppLinkUnit)
    {
        var linkBuilder = cppLinkUnit.LinkArgsBuilder as ClangLinkArgsBuilder;

        foreach (var linkFlag in cppLinkUnit.LinkFlags)
        {
            yield return linkFlag;
        }

        foreach (var argument in cppLinkUnit.LinkArgsBuilder.GetAllArguments())
        {
            yield return argument;
        }

        // Release defaults: strip symbols and drop unreferenced sections to
        // shrink the binary. Suppressed in Debug so the binary stays debuggable.
        // Modules can opt out via SetStripSymbols(false) / SetEnableDeadCodeElimination(false).
        // Note: --gc-sections only has effect when compile emitted
        // -ffunction-sections -fdata-sections (the Release default).
        if (Configuration != BuildConfiguration.Debug)
        {
            if (cppLinkUnit.LinkArgsBuilder.StripSymbols != false)
            {
                yield return "-Wl,-s";
            }
            if (cppLinkUnit.LinkArgsBuilder.EnableDeadCodeElimination != false)
            {
                yield return "-Wl,--gc-sections";
            }
        }

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
        
        if (cppLinkUnit.OutputPath.ExtensionWithDot == DynamicLibraryExtension)
        {
            // position independent code
            yield return "-fPIC";
            yield return "-shared";
        }
        else if (cppLinkUnit.OutputPath.ExtensionWithDot == StaticLibraryExtension)
        {
            yield return "-static";
        }
    }
}