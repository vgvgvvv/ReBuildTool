using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ReBuildTool.Common;

public class PlatformHelper
{
    private static string? _OSUName;
    
    public static bool IsWindows()
    {
        switch (System.Environment.OSVersion.Platform)
        {
            case PlatformID.Win32Windows:
            case PlatformID.Win32NT:
                return true;
        }
        return false;
    }

    public static bool IsLinux()
    {
        return !IsWindows() && !IsOSX();
    }

    public static bool IsOSX()
    {
        return !IsWindows() && GetOSUname() == "Darwin";
    }

    private static string GetOSUname()
    {
        if (_OSUName == null)
        {
            var checkUname = new Process();
            checkUname.StartInfo.UseShellExecute = false;
            checkUname.StartInfo.RedirectStandardOutput = true;
            checkUname.StartInfo.FileName = "uname";
            checkUname.StartInfo.Arguments = "-s";
            checkUname.Start();
            var output = checkUname.StandardOutput.ReadToEnd() ?? "";
            checkUname.WaitForExit();
            _OSUName = output.TrimEnd();
        }
        return _OSUName;
    }

    public enum Architecture
    {
        UseManagedRuntimeArchitecture,
        x86,
        x64
    }
    
    public static Architecture ManagedRuntimeArchitecture
    {
        get { return IntPtr.Size == 4 ? Architecture.x86 : Architecture.x64; }
    }
    
    public static T Pick<T>(T windows, T mac, T linux)
    {
        if (IsWindows())
            return windows;
        if (IsOSX())
            return mac;
        if (IsLinux())
            return linux;
        throw new InvalidOperationException("Unexpected Host Platform");
    }
}