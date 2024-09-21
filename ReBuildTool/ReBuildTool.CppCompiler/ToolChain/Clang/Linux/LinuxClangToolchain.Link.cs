using NiceIO;

namespace ReBuildTool.ToolChain;

public partial class LinuxClangToolchain 
{
    protected override IEnumerable<string> LinkArgsFor(CppLinkUnit cppLinkUnit)
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
        var linkBuilder = cppLinkUnit.LinkArgsBuilder as ClangLinkArgsBuilder;

        foreach (var argument in cppLinkUnit.LinkArgsBuilder.GetAllArguments())
        {
            yield return argument;
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

        foreach (var libpath in ToolChainLibraryPaths())
        {
            yield return "-L" + libpath.InQuotes();
        }
       
    }
}