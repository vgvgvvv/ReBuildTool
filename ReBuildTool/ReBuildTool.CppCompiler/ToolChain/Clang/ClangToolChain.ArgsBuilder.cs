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

	public override IEnumerable<string> WarningFlags
	{
		get
		{
			if (!WarningLevel.HasValue) yield break;
			switch (WarningLevel.Value)
			{
				case ToolChain.WarningLevel.None:
					yield return "-w";
					break;
				case ToolChain.WarningLevel.Minimal:
					// no meaningful minimal level on clang/gcc; -w matches "very quiet"
					yield return "-w";
					break;
				case ToolChain.WarningLevel.Default:
					// clang/gcc default: no flag
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

	// CRT selection (/MT /MTd /MD /MDd) is an MSVC concept; clang/gcc select
	// the C runtime implicitly via the toolchain/sysroot, so there's no flag here.
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

	// Subsystem is a Windows/MSVC concept; ld/lld ignore it.
	public override IEnumerable<string> SubsystemFlags => Enumerable.Empty<string>();

	public override IEnumerable<string> ModuleDefinitionFlags
	{
		get
		{
			// GCC/Clang use a linker version script for symbol export control.
			if (string.IsNullOrEmpty(ModuleDefinitionFile)) yield break;
			yield return $"-Wl,--version-script,{ModuleDefinitionFile}";
		}
	}

	// Manifest is a Windows/MSVC concept; ld/lld have no equivalent.
	public override IEnumerable<string> ManifestFlags => Enumerable.Empty<string>();

	// Incremental linking is MSVC-specific; ld/lld don't model it this way.
	public override IEnumerable<string> IncrementalLinkFlags => Enumerable.Empty<string>();
}

internal abstract class ClangArchiveArgsBuilder : IArchiveArgsBuilder
{
	public override void SetLto(bool enable)
	{
		// no-op: LTO not applicable to static archive (ar)
	}
}
