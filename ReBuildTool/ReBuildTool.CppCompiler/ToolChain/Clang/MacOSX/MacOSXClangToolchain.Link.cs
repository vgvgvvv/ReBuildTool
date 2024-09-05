using ReBuildTool.ToolChain.SDK;

namespace ReBuildTool.ToolChain.MacOSClangToolchain;

public partial class MacOSXClangToolchain
{
    protected override IEnumerable<string> LinkArgsFor(CppLinkUnit cppLinkUnit)
    {
        throw new NotImplementedException();
    }
    
}