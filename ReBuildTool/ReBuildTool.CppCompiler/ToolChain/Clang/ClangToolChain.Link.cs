using ResetCore.Common;

namespace ReBuildTool.ToolChain;

public abstract partial class ClangToolChain
{
	internal override CppLinkInvocation MakeLinkInvocation(CppLinkUnit cppLinkUnit)
	{
		var invocation = new CppLinkInvocation();
		invocation.ProgramName = ClangSdk.GetLinker();
		invocation.EnvVars.AddRange(EnvVars());
		invocation.Arguments.AddRange(LinkArgsFor(cppLinkUnit));
		return invocation;
	}

	protected abstract IEnumerable<string> LinkArgsFor(CppLinkUnit cppLinkUnit);
}