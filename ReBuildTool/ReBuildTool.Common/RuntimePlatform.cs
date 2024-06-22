namespace ReBuildTool.Common;

public abstract class RuntimePlatform
{
    public abstract string Name { get; }
    
    public static RuntimePlatform Current
    {
        get
        {
            if (PlatformUtils.IsWindows())
                return new WindowsDesktopRuntimePlatform();
            if (PlatformUtils.IsLinux())
                return new LinuxRuntimePlatform();
            if (PlatformUtils.IsOSX())
                return new MacOSXRuntimePlatform();
            throw new Exception("Running on unexpected OS");
        }
    }
}

public class MacOSXRuntimePlatform : RuntimePlatform
{
    public override string Name => "MacOSX";
}

public class WindowsDesktopRuntimePlatform : RuntimePlatform
{
    public override string Name => "WindowsDesktop";
}

public class LinuxRuntimePlatform : RuntimePlatform
{
    public override string Name => "Linux";
}

public class iOSRuntimePlatform : RuntimePlatform
{
    public override string Name => "iOS";
}

public class AndroidRuntimePlatform : RuntimePlatform
{
    public override string Name => "Android";
}