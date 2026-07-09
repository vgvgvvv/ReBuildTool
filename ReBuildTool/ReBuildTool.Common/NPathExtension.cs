using NiceIO;

namespace ReBuildTool.Common;

public static class NPathExtension
{
    public static string ToStandardPath(this NPath path, bool inQuotes = false)
    {
        return path.ToString().Replace('\\', '/');
    }

    /// <summary>
    /// 已知的平台目录名称列表。
    /// 如果文件路径中任何祖先目录名匹配这些名称
    /// 但不等于当前目标平台名称，则返回 false。
    /// </summary>
    private static readonly string[] PlatformNames =
    {
        "Windows", "Linux", "MacOSX", "iOS", "Android", "Wasm"
    };

    /// <summary>
    /// 检查文件是否应该被当前目标平台包含。
    /// 如果文件路径中任何祖先目录名匹配平台名称
    /// 但不等于当前目标平台，则返回 false。
    /// </summary>
    public static bool IsPlatformMatch(this NPath file, NPath scanRoot)
    {
        return IsPlatformMatch(file, scanRoot, null);
    }

    /// <summary>
    /// 检查文件是否应该被指定目标平台包含。
    /// 如果文件路径中任何祖先目录名匹配平台名称
    /// 但不等于 targetPlatform，则返回 false。
    /// </summary>
    public static bool IsPlatformMatch(this NPath file, NPath scanRoot, string? targetPlatform)
    {
        for (var dir = file.Parent; dir != scanRoot && dir != null; dir = dir.Parent)
        {
            var dirName = dir.FileName;
            if (PlatformNames.Contains(dirName) && dirName != targetPlatform)
            {
                return false;
            }
        }
        return true;
    }
}