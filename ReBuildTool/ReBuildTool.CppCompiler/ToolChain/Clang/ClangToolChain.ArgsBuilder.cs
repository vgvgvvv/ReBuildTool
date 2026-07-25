namespace ReBuildTool.ToolChain;

internal abstract class ClangCompileArgsBuilder : ICompileArgsBuilder
{
	public override void DisableException(bool enable)
	{
		// no-op: exception handling is driven by SetEnableException + ExceptionFlags
	}

	public override void DisableWarnings(string warnCode)
	{
		Append($"-Wno-{warnCode}");
	}

	public override void SetWarnAsError(bool enable)
	{
		if (enable)
		{
			Append("-Werror");
		}
	}

	public override void SetLto(bool enable)
	{
		if (enable)
		{
			Append("-flto");
		}
	}
}

internal abstract class ClangLinkArgsBuilder : ILinkArgsBuilder
{
	public override void DisableWarnings(string warnCode)
	{
		Append($"-Wno-{warnCode}");
	}

	public override void SetLto(bool enable)
	{
		if (enable)
		{
			Append("-flto");
		}
	}

	public override void SetFastLink(bool enable)
	{
		// no-op: no native fastlink equivalent on ld/lld
	}

	public override void SetWarnAsError(bool enable)
	{
		if (enable)
		{
			Append("-Werror");
		}
	}
}

internal abstract class ClangArchiveArgsBuilder : IArchiveArgsBuilder
{
	public override void SetLto(bool enable)
	{
		// no-op: LTO not applicable to static archive (ar)
	}
}
