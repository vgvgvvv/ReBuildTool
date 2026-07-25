namespace ReBuildTool.ToolChain.Wasm;

internal class WasmCompileArgsBuilder : ICompileArgsBuilder
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

	public override string CppStandardFlag
	{
		get
		{
			switch (CppStandard)
			{
				case CppVersion.Cpp11:
					return $"-std=c++11";
				case CppVersion.Cpp14:
					return $"-std=c++14";
				case CppVersion.Cpp17:
					return $"-std=c++17";
				case CppVersion.Cpp20:
					return $"-std=c++20";
				case CppVersion.Latest:
					return $"-std=c++20";
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}

	public override string RTTIFlag
	{
		get
		{
			if (EnableRTTI)
			{
				return "-frtti";
			}
			else
			{
				return "-fno-rtti";
			}
		}
	}

	public override IEnumerable<string> ExceptionFlags
	{
		get
		{
			if (EnableException)
			{
				yield return "-fexceptions";
			}
			else
			{
				yield return "-fno-exceptions";
			}
		}
	}
}

internal class WasmLinkArgsBuilder : ILinkArgsBuilder
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
		// no-op: no native fastlink equivalent on wasm-ld
	}

	public override void SetWarnAsError(bool enable)
	{
		if (enable)
		{
			Append("-Werror");
		}
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
		// no-op: LTO not applicable to static archive (ar)
	}
}
