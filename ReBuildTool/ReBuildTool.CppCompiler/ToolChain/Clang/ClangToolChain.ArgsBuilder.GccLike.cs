namespace ReBuildTool.ToolChain;

internal class GccLikeClangCompileArgsBuilder : ClangCompileArgsBuilder
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
		Append("-flto");
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
}

internal class GccLikeClangLinkArgsBuilder : ClangLinkArgsBuilder
{
	public override void DisableWarnings(string warnCode)
	{
	}

	public override void SetLto(bool enable)
	{
		Append("-flto");
	}

	public override void SetFastLink(bool enable)
	{
	}

	public override void SetWarnAsError(bool enable)
	{
	}
}

internal class GccLikeClangArchiveArgsBuilder : ClangArchiveArgsBuilder
{
	public override void SetLto(bool enable)
	{
	}
}