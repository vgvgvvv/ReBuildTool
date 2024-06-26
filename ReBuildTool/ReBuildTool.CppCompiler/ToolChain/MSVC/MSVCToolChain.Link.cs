using NiceIO;

namespace ReBuildTool.ToolChain;

public partial class MSVCToolChain 
{
	internal override CppLinkInvocation MakeLinkInvocation(CppLinkUnit cppLinkUnit)
	{
		var invocation = new CppLinkInvocation();
		// TODO:
		throw new NotImplementedException();
	}
	
	protected IEnumerable<string> DefaultLinkFlags(CppLinkUnit cppLinkUnit)
    {
    	// TODO:
    	throw new NotImplementedException();
    }
}