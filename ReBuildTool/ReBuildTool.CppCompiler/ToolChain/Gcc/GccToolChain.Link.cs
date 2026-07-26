using NiceIO;
using ResetCore.Common;

namespace ReBuildTool.ToolChain;

public partial class GccToolChain 
{
    internal override CppLinkInvocation MakeLinkInvocation(CppLinkUnit cppLinkUnit)
    {
        var invocation = new CppLinkInvocation(cppLinkUnit);
        invocation.ProgramName = LinuxSdk.GetLinker();
        invocation.EnvVars.AddRange(EnvVars());
        invocation.Arguments.AddRange(LinkArgsFor(cppLinkUnit));
        return invocation;
    }

    private IEnumerable<string> LinkArgsFor(CppLinkUnit cppLinkUnit)
    {
        yield return $"-o";
        yield return cppLinkUnit.OutputPath.ToString();

        foreach (var defaultLinkFlag in DefaultLinkFlags(cppLinkUnit))
		{
			yield return defaultLinkFlag;
		}

        yield return "@" + cppLinkUnit.ResponseFile;
    }

    protected IEnumerable<string> DefaultLinkFlags(CppLinkUnit cppLinkUnit)
    {
        var linkBuilder = cppLinkUnit.LinkArgsBuilder as GccLinkArgsBuilder;

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

        // -nostdlib
        
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

        foreach (var libraryPath in cppLinkUnit.LibraryPaths)
        {
            yield return "-L" + libraryPath;
        }

        foreach (var libpath in ToolChainLibraryPaths())
        {
            yield return "-L" + libpath;
        }
       
    }
}