using NiceIO;

namespace ReBuildTool.ToolChain.Windows;

public partial class WindowsClangToolchain
{
    protected override IEnumerable<string> ArchiveArgsFor(CppArchiveUnit unit)
    {
        yield return $"/out:{unit.OutputPath}";

        foreach (var defaultLinkFlag in DefaultArchiveFlags(unit))
        {
            yield return defaultLinkFlag;
        }

        yield return "@" + unit.ResponseFile;
    }

    protected IEnumerable<string> DefaultArchiveFlags(CppArchiveUnit cppArchiveUnit)
    {
        var linkBuilder = cppArchiveUnit.ArchiveArgsBuilder as MSVCArchiveArgsBuilder;

        yield return "/NOLOGO";

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
            yield return $"/LIBPATH:{libraryPath}";
        }
    }
}