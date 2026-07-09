namespace ReBuildTool.ToolChain.SDK;

public class MSVCConst
{
    public static IEnumerable<string> DefaultDefines(BuildConfiguration configuration, Architecture arch)
    {
        yield return "_WIN32";
        yield return "WIN32";
        yield return "WIN32_THREADS";
        yield return "_WINDOWS";
        yield return "WINDOWS";
        // NOTE: _UNICODE / UNICODE intentionally NOT defined by default. Defining
        // them makes the Windows API TCHAR macros resolve to wchar_t (the *W
        // entrypoints), but many existing C++ codebases (e.g. ResetEngine) are
        // written against the narrow-char (*A) entrypoints and pass char[] to
        // TCHAR APIs. Defining UNICODE then breaks them with char[] -> LPWSTR
        // conversion errors. Consumers that want Unicode can add these defines
        // per-module via PublicDefines.
        yield return "_CRT_SECURE_NO_WARNINGS";
        yield return "_SCL_SECURE_NO_WARNINGS";
        yield return "_WINSOCK_DEPRECATED_NO_WARNINGS";
        yield return "NOMINMAX";
		
        if (configuration == BuildConfiguration.Debug)
        {
            yield return "_DEBUG";
            yield return "DEBUG";
        }
        else
        {
            yield return "_NDEBUG";
            yield return "NDEBUG";
        }
		
        if (arch is ARMv7Architecture || arch is ARM64Architecture)
            yield return "__arm__";
    }
}