namespace ReBuildTool.ToolChain;

internal class GccCompileArgsBuilder : ICompileArgsBuilder
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
                    // gcc uses -Os for size; clang's -Oz maps to the same intent.
                    yield return "-Os";
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

    // CRT selection (/MT /MTd /MD /MDd) is an MSVC concept; gcc selects the C
    // runtime implicitly via the toolchain/sysroot, so there's no flag here.
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

internal class GccLinkArgsBuilder : ILinkArgsBuilder
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
            if (string.IsNullOrEmpty(ModuleDefinitionFile)) yield break;
            yield return $"-Wl,--version-script,{ModuleDefinitionFile}";
        }
    }

    // Manifest is a Windows/MSVC concept; ld/lld have no equivalent.
    public override IEnumerable<string> ManifestFlags => Enumerable.Empty<string>();

    // Incremental linking is MSVC-specific; ld/lld don't model it this way.
    public override IEnumerable<string> IncrementalLinkFlags => Enumerable.Empty<string>();
}

internal class GccArchiveArgsBuilder : IArchiveArgsBuilder
{
    public override void SetLto(bool enable)
    {
        // no-op: LTO not applicable to static archive (ar)
    }
}
