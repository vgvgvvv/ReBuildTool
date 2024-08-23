namespace ReBuildTool.ToolChain;

public abstract partial class ClangToolChain
{
	internal override CppArchiveInvocation MakeArchiveInvocation(CppArchiveUnit cppArchiveUnit)
	{
		return null;
	}
	
	private IEnumerable<string> ArchiveArgsFor(CppArchiveUnit unit)
	{
		return null;
	}
	
	protected IEnumerable<string> DefaultArchiveFlags(CppArchiveUnit cppArchiveUnit)
	{
		return null;
	}
}