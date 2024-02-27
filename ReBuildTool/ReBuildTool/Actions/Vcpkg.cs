using NiceIO;
using ReBuildTool.Common;
using ReBuildTool.Internal;
using ReCSharpCommon.Platform;
using ResetCore.Common;

namespace ReBuildTool.Actions;

public static class Vcpkg
{
    public static NPath VcpkgDir => GlobalPaths.IntermediaPath.Combine("Vcpkg").EnsureDirectoryExists();
    [ActionDefine("Vcpkg.Init")]
    public static void RunCmd()
    {
        Git.GetFromGit("git@github.com:microsoft/vcpkg.git", "vcpkg", VcpkgDir, "2024.02.14");
        if (PlatformUtils.GetPlatform() == PlatformType.Windows)
        {
            SimpleExec.Command.Run(VcpkgDir.Combine("vcpkg", "bootstrap-vcpkg.bat"));
        }
        else
        {
            SimpleExec.Command.Run(VcpkgDir.Combine("vcpkg", "bootstrap-vcpkg.sh"));
        }
    }
}