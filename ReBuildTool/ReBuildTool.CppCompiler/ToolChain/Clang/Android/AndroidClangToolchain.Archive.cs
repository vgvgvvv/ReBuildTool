namespace ReBuildTool.ToolChain.Android;

public partial class AndroidClangToolchain
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