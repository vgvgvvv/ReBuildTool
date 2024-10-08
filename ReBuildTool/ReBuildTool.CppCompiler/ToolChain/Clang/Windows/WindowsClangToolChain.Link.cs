using NiceIO;

namespace ReBuildTool.ToolChain.Windows;

public partial class WindowsClangToolchain
{
    protected override IEnumerable<string> LinkArgsFor(CppLinkUnit cppLinkUnit)
    {
        yield return $"/out:{cppLinkUnit.OutputPath.InQuotes()}";
		
        // TODO: manifest
        // TODO: module definition file
		
        foreach (var defaultLinkFlag in DefaultLinkFlags(cppLinkUnit))
        {
            yield return defaultLinkFlag;
        }
		
        yield return "@" + cppLinkUnit.ResponseFile.InQuotes();
    }
    
    protected IEnumerable<string> DefaultLinkFlags(CppLinkUnit cppLinkUnit)
	{
		var linkBuilder = cppLinkUnit.LinkArgsBuilder as MSVCLinkArgsBuilder;
		
	    if (Configuration == BuildConfiguration.Debug)
	    {
		    if (linkBuilder.EnableFastLink)
		    {
			    yield return "/DEBUG:FASTLINK";  
		    }
		    else
		    {
			    yield return "/DEBUG";
		    }
	    }
	    
	    // disable incremental linking
	    yield return "/INCREMENTAL:NO";
	    // support large address, only for x86
	    yield return "/LARGEADDRESSAWARE";
	    // https://learn.microsoft.com/en-us/cpp/build/reference/nxcompat-compatible-with-data-execution-prevention?view=msvc-170
	    yield return "/NXCOMPAT"; // Compatible with Data Execution Prevention
	    // use address space layout randomization
	    yield return "/DYNAMICBASE";
	    // disable logo show
	    yield return "/NOLOGO";
	    // COM id provide
	    yield return "/TLBID:1";

	    if (cppLinkUnit.OutputPath.ExtensionWithDot == DynamicLibraryExtension)
	    {
		    yield return "/DLL";
	    }

	    // Specifies whether the executable image supports high-entropy 64-bit address space layout randomization (ASLR).
	    // https://learn.microsoft.com/en-us/cpp/build/reference/highentropyva-support-64-bit-aslr?view=msvc-170
	    if (Arch is x64Architecture)
		    yield return "/HIGHENTROPYVA";
	    
	    if (Configuration != BuildConfiguration.Debug)
	    {
		    // https://learn.microsoft.com/en-us/cpp/build/reference/opt-optimizations?view=msvc-170
		    
		    yield return "/OPT:REF"; // remove unreferenced data
		    yield return "/OPT:ICF"; // reuse common function code

		    if (Arch is ARMv7Architecture)
		    {
			    /***
			     *  if the linker detects a jump to an out-of-range address,
			     * it replaces the branch instruction's destination address with the address of a code
			     * "island" that contains a branch instruction that targets the actual destination. 
			     */
			    yield return "/OPT:LBR";
		    }
	    }
	    
	    foreach (var argument in cppLinkUnit.LinkArgsBuilder.GetAllArguments())
	    {
		    yield return argument;
	    }
	    
	    foreach (var staticLibrary in ToolChainStaticLibraries())
	    {
		    yield return staticLibrary.ToNPath().InQuotes();
	    }
	    
	    foreach (var dynamicLibrary in ToolChainDynamicLibraries())
	    {
		    yield return dynamicLibrary.ToNPath().InQuotes();
	    }
	    
	    foreach (var staticLibrary in cppLinkUnit.StaticLibraries)
	    {
		    yield return staticLibrary.ToNPath().InQuotes();
	    }

	    foreach (var dynamicLibrary in cppLinkUnit.DynamicLibraries)
	    {
		    yield return dynamicLibrary.ToNPath().ChangeExtension(".lib").InQuotes();
	    }
	    
	     foreach (var libraryPath in cppLinkUnit.LibraryPaths)
        {
            yield return "/LIBPATH:" + libraryPath.InQuotes();
        }
	    
	    foreach (var libpath in ToolChainLibraryPaths()
		             .InQuotes().Select(path => "/LIBPATH:" + path))
		    yield return libpath;
    }
}