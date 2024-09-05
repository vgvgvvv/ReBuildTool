namespace ReBuildTool.ToolChain.Linux;

public partial class LinuxClangToolchain 
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