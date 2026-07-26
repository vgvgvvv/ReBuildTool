using System.Reflection;
using System.Runtime.InteropServices;
using NiceIO;
using ReBuildTool.CppCompiler;
using ReBuildTool.Common;
using ReBuildTool.Service.Global;

using ResetCore.Common;
// Disambiguate: this file lives under namespace ReBuildTool.ToolChain.* which
// would otherwise shadow System.Runtime.InteropServices.Architecture with the
// project's own Architecture type.
using SystemArch = System.Runtime.InteropServices.Architecture;

namespace ReBuildTool.ToolChain.SDK.Ninja;

/// <summary>
/// Ninja build backend runner + executable discovery. Mirrors
/// <c>SDK/MakeFile/MakeFile.cs</c>: one static class with a "find the binary"
/// helper and a "run it" helper. The generator half lives in
/// <c>CppBuilder.Process.NinjaFile.cs</c> + <c>NinjaFileGenerator</c>.
/// <para>
/// Discovery priority (ninja is a <b>host</b> tool, independent of the target
/// platform/arch we are compiling for — hence no <c>BuildOptions</c> parameter,
/// unlike <c>MakeFile.GetMakeFileExecutable</c>):
/// </para>
/// <list type="number">
/// <item><c>Vendor/ninja/&lt;host&gt;/[ninja|ninja.exe]</c> located by walking up
/// from the entry assembly — this is the authoritative copy shipped in the repo
/// and is always present after a normal install/clone.</item>
/// <item>A <c>ninja</c>/<c>ninja.exe</c> resolvable on <c>PATH</c> (lets users
/// override with a newer build).</item>
/// </list>
/// </summary>
public static class Ninja
{
    // Host platform → Vendor/ninja subdirectory + binary name. Derived from the
    // actual layout under Vendor/ninja/ (ninja-win, ninja-linux,
    // ninja-linux-aarch64, ninja-mac). ninja-mac is a universal binary covering
    // both x64 and arm64 macOS.
    private const string WindowsSubdir = "ninja-win";
    private const string WindowsBinary = "ninja.exe";
    private const string LinuxX64Subdir = "ninja-linux";
    private const string LinuxArm64Subdir = "ninja-linux-aarch64";
    private const string PosixBinary = "ninja";
    private const string MacSubdir = "ninja-mac";

    /// <summary>
    /// Resolves the ninja executable to invoke. Never throws on "not found" —
    /// returns an <see cref="NPath"/> that may or may not <see cref="NPath.Exists"/>,
    /// so callers can branch and report. (Matches the spirit of
    /// <c>MakeFile.GetMakeFileExecutable</c>, which just returns a path that the
    /// runner then existence-checks.)
    /// </summary>
    public static NPath GetNinjaExecutable()
    {
        // 1) Authoritative Vendor/ninja/<host>/ copy.
        var vendored = FindVendoredNinja();
        if (vendored != null && vendored.Exists())
        {
            return vendored;
        }

        // 2) PATH fallback (lets a user-supplied ninja win).
        var onPath = FindNinjaOnPath();
        if (onPath != null && onPath.Exists())
        {
            return onPath;
        }

        // Return the vendored path (even though missing) so the caller's error
        // message points at where we *expected* ninja to be.
        return vendored ?? new NPath("ninja");
    }

    /// <summary>
    /// Runs <c>ninja -f &lt;buildFile&gt; -j&lt;N&gt;</c> (plus <c>-n</c> in dry-run
    /// mode). The workspace is the build file's parent directory so relative paths
    /// inside build.ninja (if any) resolve naturally. <paramref name="env"/> is
    /// merged into the child process environment — this is how the MSVC toolchain's
    /// PATH/VSLANG (see <see cref="ReBuildTool.ToolChain.IToolChain.EnvVars"/>) reach
    /// the compiler ninja spawns; the MakeFile backend skips this and relies on the
    /// outer shell having the env already set, which is fragile.
    /// </summary>
    public static bool RunNinja(NPath buildFile,
        Dictionary<string, string>? env = null,
        int? maxJobs = null,
        bool dryRun = false)
    {
        var exe = GetNinjaExecutable();
        if (!exe.Exists())
        {
            Log.Error($"cannot find ninja executable. Looked under Vendor/ninja/ (relative to rbt) " +
                      $"and on PATH. Either run from a full repo checkout (with Vendor/ninja/ present) " +
                      $"or install ninja and ensure it is on PATH. Resolved path: {exe}");
            return false;
        }

        int jCount = Math.Max(1, maxJobs ?? CppCompilerArgs.Get().MaxJobs.Value);

        // -f is mandatory: without it ninja looks for build.ninja in the CWD,
        // which we already point at via WithWorkspace, but being explicit avoids
        // surprises if a stray build.ninja ever lands in the workspace.
        var shell = Shell.Create()
            .WithProgram(exe)
            .WithWorkspace(buildFile.Parent);
        shell.AppendArgument("-f");
        shell.AppendArgument(buildFile.ToString());
        shell.AppendArgument($"-j{jCount}");
        if (dryRun)
        {
            shell.AppendArgument("-n");
        }

        if (env != null)
        {
            shell.WithEnvVars(env);
        }

        Log.Info($"Run ninja: {buildFile.FileName} (-j{jCount}{(dryRun ? ", dry-run" : "")})");
        shell.Execute().WaitForEnd();
        if (shell.Process == null || shell.Process.ExitCode != 0)
        {
            Log.Error($"ninja {buildFile} failed");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Walks up from the entry assembly's directory looking for an ancestor that
    /// contains <c>Vendor/ninja/</c>, then maps the host platform to the right
    /// subdirectory. Covers both the dev layout (rbt running from
    /// <c>Binary/&lt;Plat&gt;/ReBuildTool/</c>) and the installed layout (repo at
    /// <c>~/.rbt/ReBuildTool/</c>). Caps the walk at a sane depth so a misnamed
    /// tree doesn't scan the whole drive.
    /// </summary>
    private static NPath? FindVendoredNinja()
    {
        var entryLocation = Assembly.GetEntryAssembly()?.Location;
        if (string.IsNullOrEmpty(entryLocation))
        {
            return null;
        }

        var (subdir, binary) = HostSubdirAndBinary();
        if (subdir == null)
        {
            // Unsupported host — let the PATH fallback have a go.
            return null;
        }

        NPath? dir = new NPath(entryLocation).Parent;
        // Up to 6 levels covers Binary/<Plat>/ReBuildTool/ (3) + slack for
        // unconventional installs.
        for (int i = 0; i < 6 && dir != null; i++)
        {
            var candidate = dir.Combine("Vendor", "ninja", subdir, binary);
            if (candidate.Exists())
            {
                return candidate;
            }
            // Stop at the filesystem root (where Parent would equal dir) or when
            // there's nothing above us left to walk.
            var parent = dir.Parent;
            if (parent == null || parent.ToString() == dir.ToString())
            {
                break;
            }
            dir = parent;
        }
        return null;
    }

    private static NPath? FindNinjaOnPath()
    {
        // Prefer the platform-appropriate name; if neither exists on PATH we
        // return the bare name and let RunNinja's existence check produce the
        // final error.
        var name = PlatformHelper.IsWindows() ? "ninja.exe" : "ninja";
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathEnv))
        {
            return null;
        }
        foreach (var raw in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = raw.Trim().Trim('"');
            if (trimmed.Length == 0)
            {
                continue;
            }
            var candidate = new NPath(trimmed).Combine(name);
            if (candidate.Exists())
            {
                return candidate;
            }
        }
        return null;
    }

    /// <summary>Maps the running host OS+arch to its Vendor/ninja subdirectory.</summary>
    private static (string? subdir, string binary) HostSubdirAndBinary()
    {
        if (PlatformHelper.IsWindows())
        {
            return (WindowsSubdir, WindowsBinary);
        }
        if (PlatformHelper.IsOSX())
        {
            // ninja-mac is a universal binary; both x64 and arm64 hosts use it.
            return (MacSubdir, PosixBinary);
        }
        if (PlatformHelper.IsLinux())
        {
            // ProcessArchitecture distinguishes the two ninja-linux variants.
            // (NOT BuildOptions.Architecture — that's the *target* we compile for.)
            // Fully-qualify System.Runtime.InteropServices.Architecture because the
            // enclosing namespace is under ReBuildTool.ToolChain, which shadows it
            // with the project's own Architecture type.
            return (RuntimeInformation.ProcessArchitecture == SystemArch.Arm64
                ? LinuxArm64Subdir
                : LinuxX64Subdir, PosixBinary);
        }
        return (null, PosixBinary);
    }
}
