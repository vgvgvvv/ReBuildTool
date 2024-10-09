namespace ReBuildTool.ToolChain;

internal class MSVCCompileArgsBuilder : ICompileArgsBuilder
{
	public override void DisableException(bool enable)
	{
		
	}

	public override void DisableWarnings(string warnCode)
	{
		Append($"/wd{warnCode}");
	}

	public override void SetWarnAsError(bool enable)
	{
		Append("/WX");
	}

	public override void SetLto(bool enable)
	{
		Append("/GL"); // Whole program optimization
	}

	public override string CppStandardFlag
	{
		get
		{
			switch (CppStandard)
			{
				case CppVersion.Cpp11:
					return $"/std:C++11";
				case CppVersion.Cpp14:
					return $"/std:C++14";
				case CppVersion.Cpp17:
					return $"/std:C++17";
				case CppVersion.Cpp20:
					return $"/std:C++20";
				case CppVersion.Latest:
					return $"/std:C++latest";
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}

internal class MSVCLinkArgsBuilder : ILinkArgsBuilder
{
	public override void DisableWarnings(string warnCode)
	{
		Append($"/IGNORE:{warnCode}");
	}

	public override void SetLto(bool enable)
	{
		Append("/LTCG");
		Append($"/CGTHREADS:{Environment.ProcessorCount}");
	}

	public override void SetFastLink(bool enable)
	{
		EnableFastLink = enable;
	}

	public override void SetWarnAsError(bool enable)
	{
		Append("/WX");
	}
	
	public void DisableDefaultLib()
	{
		Append("/NODEFAULTLIB");
	}

	public override IEnumerable<string> GetAllArguments()
	{
		foreach (var argument in base.GetAllArguments())
		{
			yield return argument;
		}
	}
	
	public bool EnableFastLink { get; private set; }
}

internal class MSVCArchiveArgsBuilder : IArchiveArgsBuilder
{
	public override void SetLto(bool enable)
	{
		Append("/LTCG");
	}
}