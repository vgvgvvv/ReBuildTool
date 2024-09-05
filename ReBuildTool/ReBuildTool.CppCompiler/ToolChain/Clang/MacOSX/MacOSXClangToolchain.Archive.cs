namespace ReBuildTool.ToolChain.MacOSClangToolchain;

public partial class MacOSXClangToolchain
{
    protected override IEnumerable<string> ArchiveArgsFor(CppArchiveUnit unit)
    {
        throw new NotImplementedException();
    }

    protected IEnumerable<string> DefaultArchiveFlags(CppArchiveUnit cppArchiveUnit)
    {
        throw new NotImplementedException();
    }
}