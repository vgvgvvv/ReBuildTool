namespace ReBuildTool.ToolChain;

internal class GccCompileArgsBuilder : ICompileArgsBuilder
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

internal class GccLinkArgsBuilder : ILinkArgsBuilder
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

internal class GccArchiveArgsBuilder : IArchiveArgsBuilder
{
    public override void SetLto(bool enable)
    {
        // TODO:
    }
}