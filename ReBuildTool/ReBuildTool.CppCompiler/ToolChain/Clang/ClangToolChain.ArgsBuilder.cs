namespace ReBuildTool.ToolChain;

internal class ClangCompileArgsBuilder : ICompileArgsBuilder
{
	public override void DisableException(bool enable)
	{
		// TODO:
	}

	public override void DisableWarnings(string warnCode)
	{
		// TODO:
	}

	public override void SetWarnAsError(bool enable)
	{
		// TODO:
	}

	public override void SetLto(bool enable)
	{
		// TODO:
	}
}

internal class ClangLinkArgsBuilder : ILinkArgsBuilder
{
	public override void DisableWarnings(string warnCode)
	{
		// TODO:
	}

	public override void SetLto(bool enable)
	{
		// TODO:
	}

	public override void SetFastLink(bool enable)
	{
		// TODO:
	}

	public override void SetWarnAsError(bool enable)
	{
		// TODO:
	}
}

internal class ClangArchiveArgsBuilder : IArchiveArgsBuilder
{
	public override void SetLto(bool enable)
	{
		// TODO:
	}
}