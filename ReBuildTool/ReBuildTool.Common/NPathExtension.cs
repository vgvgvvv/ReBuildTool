using NiceIO;

namespace ReBuildTool.Common;

public static class NPathExtension
{
    public static string ToStandardPath(this NPath path, bool inQuotes = false)
    {
        return path.ToString().Replace('\\', '/');
    }
}