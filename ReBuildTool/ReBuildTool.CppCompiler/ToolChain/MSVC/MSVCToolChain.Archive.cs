using NiceIO;

namespace ReBuildTool.ToolChain;

public partial class MSVCToolChain 
{
	internal override CppArchiveInvocation MakeArchiveInvocation(CppArchiveUnit cppArchiveUnit)
	{
		var invocation = new CppArchiveInvocation();
		// TODO:
		throw new NotImplementedException();
	}
	
	protected IEnumerable<string> DefaultArchiveFlags(CppArchiveUnit cppArchiveUnit)
	{
		// TODO:
		throw new NotImplementedException();		
	}
}