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
        yield return "_UNICODE";
        yield return "UNICODE";
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