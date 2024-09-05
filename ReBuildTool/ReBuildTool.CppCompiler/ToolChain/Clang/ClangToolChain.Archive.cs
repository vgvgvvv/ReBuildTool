using ResetCore.Common;

namespace ReBuildTool.ToolChain;

public abstract partial class ClangToolChain
{
	internal override CppArchiveInvocation MakeArchiveInvocation(CppArchiveUnit cppArchiveUnit)
	{
		var invocation = new CppArchiveInvocation();
		invocation.ProgramName = ClangSdk.GetArchiver();
		invocation.EnvVars.AddRange(EnvVars());
		invocation.Arguments.AddRange(ArchiveArgsFor(cppArchiveUnit));
		return invocation;
	}

	protected abstract IEnumerable<string> ArchiveArgsFor(CppArchiveUnit unit);

}