namespace ReBuildTool.ToolChain.Wasm;

internal class WasmCompileArgsBuilder : ICompileArgsBuilder
{
	public override void DisableException(bool enable)
	{
		throw new NotImplementedException();
	}

	public override void DisableWarnings(string warnCode)
	{
		throw new NotImplementedException();
	}

	public override void SetWarnAsError(bool enable)
	{
		throw new NotImplementedException();
	}

	public override void SetLto(bool enable)
	{
		throw new NotImplementedException();
	}
}

internal class WasmLinkArgsBuilder : ILinkArgsBuilder
{
	public override void DisableWarnings(string warnCode)
	{
		throw new NotImplementedException();
	}

	public override void SetLto(bool enable)
	{
		throw new NotImplementedException();
	}

	public override void SetFastLink(bool enable)
	{
		throw new NotImplementedException();
	}

	public override void SetWarnAsError(bool enable)
	{
		throw new NotImplementedException();
	}
	
	public void DisableDefaultLib()
	{
		throw new NotImplementedException();
	}

	public override IEnumerable<string> GetAllArguments()
	{
		foreach (var argument in base.GetAllArguments())
		{
			yield return argument;
		}
	}
	
}

internal class WasmArchiveArgsBuilder : IArchiveArgsBuilder
{
	public override void SetLto(bool enable)
	{
		throw new NotImplementedException();
	}
}