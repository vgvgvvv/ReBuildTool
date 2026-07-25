namespace ReBuildTool.ToolChain;

internal class MSVCCompileArgsBuilder : ICompileArgsBuilder
{
	public override void DisableException(bool enable)
	{
		// TODO: no-op dead code; exception handling is driven by SetEnableException + ExceptionFlags
	}

	public override void DisableWarnings(string warnCode)
	{
		Append($"/wd{warnCode}");
	}

	public override void SetWarnAsError(bool enable)
	{
		if (enable)
		{
			Append("/WX");
		}
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
					return $"/std:c++11";
				case CppVersion.Cpp14:
					return $"/std:c++14";
				case CppVersion.Cpp17:
					return $"/std:c++17";
				case CppVersion.Cpp20:
					return $"/std:c++20";
				case CppVersion.Latest:
					return $"/std:c++latest";
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
				return "/GR";
			}
			else
			{
				return "/GR-";
			}
		}
	}

	public override IEnumerable<string> ExceptionFlags
	{
		get
		{
			if (EnableException)
			{
				yield return "/EHa";
				yield return "/D_HAS_EXCEPTIONS=1";
			}
			else
			{
				yield return "/EHa-";
				yield return "/D_HAS_EXCEPTIONS=0";
			}
		}
	}

	public override IEnumerable<string> WarningFlags
	{
		get
		{
			if (!WarningLevel.HasValue) yield break;
			switch (WarningLevel.Value)
			{
				case global::ReBuildTool.ToolChain.WarningLevel.None:
					yield return "/W0";
					break;
				case global::ReBuildTool.ToolChain.WarningLevel.Minimal:
					yield return "/W1";
					break;
				case global::ReBuildTool.ToolChain.WarningLevel.Default:
					yield return "/W3";
					break;
				case global::ReBuildTool.ToolChain.WarningLevel.All:
				case global::ReBuildTool.ToolChain.WarningLevel.Extra:
					yield return "/W4";
					break;
				case global::ReBuildTool.ToolChain.WarningLevel.Pedantic:
					yield return "/Wall";
					break;
				default:
					yield return "/W3";
					break;
			}
		}
	}

	public override IEnumerable<string> OptimizationFlags
	{
		get
		{
			if (!OptimizationLevel.HasValue) yield break;
			switch (OptimizationLevel.Value)
			{
				case global::ReBuildTool.ToolChain.OptimizationLevel.None:
					yield return "/Od";
					break;
				case global::ReBuildTool.ToolChain.OptimizationLevel.Size:
					yield return "/O1";
					break;
				case global::ReBuildTool.ToolChain.OptimizationLevel.Speed:
					yield return "/O2";
					break;
				case global::ReBuildTool.ToolChain.OptimizationLevel.MaxSpeed:
					yield return "/Ox";
					break;
			}
		}
	}

	public override IEnumerable<string> CRunTimeFlags
	{
		get
		{
			if (!CRunTimeType.HasValue) yield break;
			switch (CRunTimeType.Value)
			{
				case global::ReBuildTool.ToolChain.CRunTimeType.DebugStatic:
					yield return "/MTd";
					break;
				case global::ReBuildTool.ToolChain.CRunTimeType.ReleaseStatic:
					yield return "/MT";
					break;
				case global::ReBuildTool.ToolChain.CRunTimeType.DebugDynamic:
					yield return "/MDd";
					break;
				case global::ReBuildTool.ToolChain.CRunTimeType.ReleaseDynamic:
					yield return "/MD";
					break;
			}
		}
	}

	// MSVC/PE doesn't use position-independent code; PIC is a no-op here.
	public override IEnumerable<string> PicFlags => Enumerable.Empty<string>();
}

internal class MSVCLinkArgsBuilder : ILinkArgsBuilder
{
	public override void DisableWarnings(string warnCode)
	{
		Append($"/IGNORE:{warnCode}");
	}

	public override void SetLto(bool enable)
	{
		if (enable)
		{
			Append("/LTCG");
			Append($"/CGTHREADS:{Environment.ProcessorCount}");
		}
	}

	public override void SetFastLink(bool enable)
	{
		EnableFastLink = enable;
	}

	public override void SetWarnAsError(bool enable)
	{
		if (enable)
		{
			Append("/WX");
		}
	}

	public void DisableDefaultLib()
	{
		Append("/NODEFAULTLIB");
	}

	public override IEnumerable<string> SubsystemFlags
	{
		get
		{
			if (!Subsystem.HasValue) yield break;
			yield return Subsystem.Value == ToolChain.Subsystem.Console
				? "/SUBSYSTEM:CONSOLE"
				: "/SUBSYSTEM:WINDOWS";
		}
	}

	public override IEnumerable<string> ModuleDefinitionFlags
	{
		get
		{
			if (string.IsNullOrEmpty(ModuleDefinitionFile)) yield break;
			yield return $"/DEF:{ModuleDefinitionFile}";
		}
	}

	public override IEnumerable<string> ManifestFlags
	{
		get
		{
			if (!GenerateManifest.HasValue) yield break;
			yield return GenerateManifest.Value ? "/MANIFEST" : "/MANIFEST:NO";
		}
	}

	public override IEnumerable<string> IncrementalLinkFlags
	{
		get
		{
			if (!EnableIncrementalLink.HasValue) yield break;
			yield return EnableIncrementalLink.Value ? "/INCREMENTAL" : "/INCREMENTAL:NO";
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