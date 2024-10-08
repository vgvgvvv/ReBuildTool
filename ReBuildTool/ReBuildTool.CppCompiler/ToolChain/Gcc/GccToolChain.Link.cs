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
        yield return cppLinkUnit.OutputPath.InQuotes();
        
        foreach (var defaultLinkFlag in DefaultLinkFlags(cppLinkUnit))
		{
			yield return defaultLinkFlag;
		}
        
        yield return "@" + cppLinkUnit.ResponseFile.InQuotes();
    }

    protected IEnumerable<string> DefaultLinkFlags(CppLinkUnit cppLinkUnit)
    {
        var linkBuilder = cppLinkUnit.LinkArgsBuilder as GccLinkArgsBuilder;

        foreach (var argument in cppLinkUnit.LinkArgsBuilder.GetAllArguments())
        {
            yield return argument;
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
            yield return "-l" + staticLibrary.ToNPath().InQuotes();
        }
	    
        foreach (var dynamicLibrary in ToolChainDynamicLibraries())
        {
            yield return "-l" + dynamicLibrary.ToNPath().InQuotes();
        }
	    
        foreach (var staticLibrary in cppLinkUnit.StaticLibraries)
        {
            yield return "-l" + staticLibrary.ToNPath().InQuotes();
        }

        foreach (var dynamicLibrary in cppLinkUnit.DynamicLibraries)
        {
            yield return "-l" + dynamicLibrary.ToNPath().InQuotes();
        }
        
        foreach (var libraryPath in cppLinkUnit.LibraryPaths)
        {
            yield return "-L" + libraryPath.InQuotes();
        }

        foreach (var libpath in ToolChainLibraryPaths())
        {
            yield return "-L" + libpath.InQuotes();
        }
       
    }
}