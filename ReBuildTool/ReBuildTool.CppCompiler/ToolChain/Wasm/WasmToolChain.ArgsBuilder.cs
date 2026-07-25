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

	// Note: the Wasm toolchain's Compile/Link/Archive invocations are not yet
	// implemented (MakeLinkArgsBuilder/MakeArchiveArgsBuilder throw), so the
	// flags below are only consumed if/when Wasm compile is wired up.

	public override IEnumerable<string> WarningFlags
	{
		get
		{
			if (!WarningLevel.HasValue) yield break;
			switch (WarningLevel.Value)
			{
				case ToolChain.WarningLevel.None:
				case ToolChain.WarningLevel.Minimal:
					yield return "-w";
					break;
				case ToolChain.WarningLevel.Default:
					yield break;
				case ToolChain.WarningLevel.All:
					yield return "-Wall";
					break;
				case ToolChain.WarningLevel.Extra:
					yield return "-Wall";
					yield return "-Wextra";
					break;
				case ToolChain.WarningLevel.Pedantic:
					yield return "-Wall";
					yield return "-Wextra";
					yield return "-Wpedantic";
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
				case ToolChain.OptimizationLevel.None:
					yield return "-O0";
					break;
				case ToolChain.OptimizationLevel.Size:
					yield return "-Oz";
					break;
				case ToolChain.OptimizationLevel.Speed:
					yield return "-O2";
					break;
				case ToolChain.OptimizationLevel.MaxSpeed:
					yield return "-O3";
					break;
			}
		}
	}

	// CRT selection is an MSVC concept; emcc selects the C runtime implicitly.
	public override IEnumerable<string> CRunTimeFlags => Enumerable.Empty<string>();

	public override IEnumerable<string> PicFlags
	{
		get
		{
			if (!EnablePIC.HasValue) yield break;
			yield return EnablePIC.Value ? "-fPIC" : "-fno-pic";
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

	// Subsystem/manifest/incremental are Windows/MSVC concepts; wasm-ld has none.
	public override IEnumerable<string> SubsystemFlags => Enumerable.Empty<string>();

	public override IEnumerable<string> ModuleDefinitionFlags
	{
		get
		{
			if (string.IsNullOrEmpty(ModuleDefinitionFile)) yield break;
			yield return $"--export-table,{ModuleDefinitionFile}";
		}
	}

	public override IEnumerable<string> ManifestFlags => Enumerable.Empty<string>();

	public override IEnumerable<string> IncrementalLinkFlags => Enumerable.Empty<string>();
}

internal class WasmArchiveArgsBuilder : IArchiveArgsBuilder
{
	public override void SetLto(bool enable)
	{
		// no-op: LTO not applicable to static archive (ar)
	}
}
