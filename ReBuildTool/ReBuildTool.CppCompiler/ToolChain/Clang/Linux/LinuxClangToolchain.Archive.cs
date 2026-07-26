using NiceIO;

namespace ReBuildTool.ToolChain;

public partial class LinuxClangToolchain 
{
    protected override IEnumerable<string> ArchiveArgsFor(CppArchiveUnit unit)
    {
        yield return $"-o";
        yield return unit.OutputPath.ToString();

        foreach (var defaultLinkFlag in DefaultArchiveFlags(unit))
        {
            yield return defaultLinkFlag;
        }

        yield return "@" + unit.ResponseFile;
    }

    protected IEnumerable<string> DefaultArchiveFlags(CppArchiveUnit cppArchiveUnit)
    {
        var linkBuilder = cppArchiveUnit.ArchiveArgsBuilder;

        foreach (var argument in linkBuilder.GetAllArguments())
        {
            yield return argument;
        }

        foreach (var archiveFlag in cppArchiveUnit.ArchiveFlags)
        {
            yield return archiveFlag;
        }

        foreach (var staticLibrary in cppArchiveUnit.StaticLibraries)
        {
            yield return staticLibrary.ToNPath().ToString();
        }

        foreach (var libraryPath in cppArchiveUnit.LibraryPaths)
        {
            yield return $"-L{libraryPath}";
        }

        foreach (var libpath in ToolChainLibraryPaths())
        {
            yield return $"-L{libpath}";
        }
    }
}