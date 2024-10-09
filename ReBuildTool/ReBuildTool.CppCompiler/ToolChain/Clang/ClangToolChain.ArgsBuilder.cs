namespace ReBuildTool.ToolChain;

internal abstract class ClangCompileArgsBuilder : ICompileArgsBuilder
{
	public override void DisableException(bool enable)
	{
	}

	public override void DisableWarnings(string warnCode)
	{
	}

	public override void SetWarnAsError(bool enable)
	{
	}

	public override void SetLto(bool enable)
	{
	}
}

internal abstract class ClangLinkArgsBuilder : ILinkArgsBuilder
{
	public override void DisableWarnings(string warnCode)
	{
	}

	public override void SetLto(bool enable)
	{
	}

	public override void SetFastLink(bool enable)
	{
	}

	public override void SetWarnAsError(bool enable)
	{
	}
}

internal abstract class ClangArchiveArgsBuilder : IArchiveArgsBuilder
{
	public override void SetLto(bool enable)
	{
	}
}