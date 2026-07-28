using System.Diagnostics;
using NiceIO;
using ReBuildTool.Service.PackageService;

namespace ReBuildTool.Test;

/// <summary>
/// End-to-end restore against a real git repository.
///
/// The repository is created locally and cloned over a filesystem path, so the test exercises the
/// genuine clone / fetch / rev-parse / reset code path without ever touching the network - CI runs
/// this on three hosts and a flaky external dependency would be worse than no test at all.
/// </summary>
[TestFixture]
public class TestPackageRestore
{
    private NPath WorkDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        WorkDirectory = Path.Combine(Path.GetTempPath(), $"rbt-restore-{Guid.NewGuid():N}")
            .ToNPath()
            .EnsureDirectoryExists();
    }

    [TearDown]
    public void TearDown()
    {
        // A clone contains read-only object files on Windows; ignore whatever will not go away.
        try
        {
            WorkDirectory.DeleteIfExists(DeleteMode.Normal);
        }
        catch (Exception)
        {
            // Leaving a temp directory behind must never fail a test run.
        }
    }

    private static string Git(NPath workingDirectory, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = workingDirectory.ToString(),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        // A committing identity is configured per-command: CI runners have no global git identity.
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add("user.email=rbt@example.com");
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add("user.name=rbt test");
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)!;
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        Assert.That(process.ExitCode, Is.EqualTo(0),
            $"git {string.Join(" ", arguments)} failed: {stderr}");
        return stdout.Trim();
    }

    /// <summary>Builds a git repository holding one rbt module, tagged v1.0.</summary>
    private NPath CreateLibraryRepository(string name)
    {
        var repository = WorkDirectory.Combine($"{name}Repo").EnsureDirectoryExists();
        repository.Combine($"{name}.module.cs").WriteAllText(
            $"using ReBuildTool.ToolChain;{Environment.NewLine}" +
            $"public class {name} : CppModuleRule {{ }}{Environment.NewLine}");
        PackageManifest.PathIn(repository).WriteAllText($"{{ \"name\": \"{name}\" }}");

        Git(repository, "init", "--initial-branch=main");
        Git(repository, "add", ".");
        Git(repository, "commit", "-m", "initial");
        Git(repository, "tag", "v1.0");
        return repository;
    }

    private NPath CreateProject(string manifestJson)
    {
        var project = WorkDirectory.Combine("Project").EnsureDirectoryExists();
        PackageManifest.PathIn(project).WriteAllText(manifestJson);
        return project;
    }

    [Test]
    public void AGitPackageIsClonedAndPinnedToItsTag()
    {
        var repository = CreateLibraryRepository("GreeterLib");
        var expectedSha = Git(repository, "rev-parse", "v1.0^{commit}");
        var project = CreateProject(
            "{ \"dependencies\": { \"GreeterLib\": { " +
            $"\"git\": \"{repository.ToString(SlashMode.Forward)}\", \"tag\": \"v1.0\" }} }} }}");

        var result = new PackageRestoreService().Restore(project, new PackageRestoreOptions());

        Assert.That(result.Packages.Select(package => package.Name), Is.EqualTo(new[] { "GreeterLib" }));
        Assert.That(project.Combine("Packages", "GreeterLib", "GreeterLib.module.cs").FileExists(), Is.True);

        // The lock records the commit the tag pointed at, not the tag: upstream can move a tag.
        var lockFile = PackageLockFile.ReadFrom(project);
        Assert.That(lockFile, Is.Not.Null);
        Assert.That(lockFile!.Find("GreeterLib")!.Resolved, Is.EqualTo(expectedSha));
    }

    [Test]
    public void ASecondRestoreSucceedsOffline()
    {
        var repository = CreateLibraryRepository("GreeterLib");
        var project = CreateProject(
            "{ \"dependencies\": { \"GreeterLib\": { " +
            $"\"git\": \"{repository.ToString(SlashMode.Forward)}\", \"tag\": \"v1.0\" }} }} }}");
        new PackageRestoreService().Restore(project, new PackageRestoreOptions());

        // Everything is already on disk and the lock pins a commit, so no remote access is needed.
        Assert.DoesNotThrow(() =>
            new PackageRestoreService().Restore(project, new PackageRestoreOptions { Offline = true }));
    }

    /// <summary>
    /// Bumping a dependency's tag has to actually move the checkout.
    ///
    /// The fetcher reuses the commit the lock recorded rather than asking the remote again - that
    /// is what keeps an ordinary build reproducible and offline. Handing it a lock entry from a
    /// different pin turns that shortcut into a trap: the old tag's commit still resolves locally,
    /// so the build silently stays on the previous version while the manifest says otherwise.
    /// </summary>
    [Test]
    public void ChangingTheTagReResolvesInsteadOfReusingTheLock()
    {
        var repository = CreateLibraryRepository("GreeterLib");
        var project = CreateProject(
            "{ \"dependencies\": { \"GreeterLib\": { " +
            $"\"git\": \"{repository.ToString(SlashMode.Forward)}\", \"tag\": \"v1.0\" }} }} }}");
        new PackageRestoreService().Restore(project, new PackageRestoreOptions());
        var firstSha = PackageLockFile.ReadFrom(project)!.Find("GreeterLib")!.Resolved;

        // A second release upstream, tagged v2.0.
        repository.Combine("GreeterLib.module.cs").WriteAllText(
            $"using ReBuildTool.ToolChain;{Environment.NewLine}" +
            $"public class GreeterLib : CppModuleRule {{ /* v2 */ }}{Environment.NewLine}");
        Git(repository, "add", ".");
        Git(repository, "commit", "-m", "second");
        Git(repository, "tag", "v2.0");
        var secondSha = Git(repository, "rev-parse", "v2.0^{commit}");
        Assert.That(secondSha, Is.Not.EqualTo(firstSha));

        PackageManifest.PathIn(project).WriteAllText(
            "{ \"dependencies\": { \"GreeterLib\": { " +
            $"\"git\": \"{repository.ToString(SlashMode.Forward)}\", \"tag\": \"v2.0\" }} }} }}");
        new PackageRestoreService().Restore(project, new PackageRestoreOptions());

        Assert.That(PackageLockFile.ReadFrom(project)!.Find("GreeterLib")!.Resolved,
            Is.EqualTo(secondSha), "the new tag should have been resolved, not the locked commit");
        Assert.That(project.Combine("Packages", "GreeterLib", "GreeterLib.module.cs").ReadAllText(),
            Does.Contain("v2"), "the working tree should have moved to the new commit");
    }

    [Test]
    public void AnUnfetchedPackageCannotBeRestoredOffline()
    {
        var repository = CreateLibraryRepository("GreeterLib");
        var project = CreateProject(
            "{ \"dependencies\": { \"GreeterLib\": { " +
            $"\"git\": \"{repository.ToString(SlashMode.Forward)}\", \"tag\": \"v1.0\" }} }} }}");

        var exception = Assert.Throws<PackageException>(() =>
            new PackageRestoreService().Restore(project, new PackageRestoreOptions { Offline = true }));

        Assert.That(exception!.Message, Does.Contain("Offline"));
    }

    [Test]
    public void TransitiveGitDependenciesAreFollowed()
    {
        var leaf = CreateLibraryRepository("LeafLib");
        var middle = WorkDirectory.Combine("MiddleLibRepo").EnsureDirectoryExists();
        middle.Combine("MiddleLib.module.cs").WriteAllText(
            $"using ReBuildTool.ToolChain;{Environment.NewLine}public class MiddleLib : CppModuleRule {{ }}");
        PackageManifest.PathIn(middle).WriteAllText(
            "{ \"name\": \"MiddleLib\", \"dependencies\": { \"LeafLib\": { " +
            $"\"git\": \"{leaf.ToString(SlashMode.Forward)}\", \"tag\": \"v1.0\" }} }} }}");
        Git(middle, "init", "--initial-branch=main");
        Git(middle, "add", ".");
        Git(middle, "commit", "-m", "initial");
        Git(middle, "tag", "v1.0");

        var project = CreateProject(
            "{ \"dependencies\": { \"MiddleLib\": { " +
            $"\"git\": \"{middle.ToString(SlashMode.Forward)}\", \"tag\": \"v1.0\" }} }} }}");

        var result = new PackageRestoreService().Restore(project, new PackageRestoreOptions());

        // LeafLib is only discoverable by reading MiddleLib's manifest after it was cloned.
        Assert.That(
            result.Packages.Select(package => package.Name).OrderBy(name => name),
            Is.EqualTo(new[] { "LeafLib", "MiddleLib" }));
        Assert.That(project.Combine("Packages", "LeafLib").DirectoryExists(), Is.True);
    }

    [Test]
    public void AProjectWithoutAManifestIsUntouched()
    {
        var project = WorkDirectory.Combine("Bare").EnsureDirectoryExists();

        var result = new PackageRestoreService().Restore(project, new PackageRestoreOptions());

        // Projects that do not use packages must not gain a Packages/ directory or a lock file.
        Assert.That(result.Packages, Is.Empty);
        Assert.That(project.Combine("Packages").DirectoryExists(), Is.False);
        Assert.That(PackageLockFile.PathIn(project).FileExists(), Is.False);
    }

    [Test]
    public void RestoreAddsPackagesToGitIgnore()
    {
        var repository = CreateLibraryRepository("GreeterLib");
        var project = CreateProject(
            "{ \"dependencies\": { \"GreeterLib\": { " +
            $"\"git\": \"{repository.ToString(SlashMode.Forward)}\", \"tag\": \"v1.0\" }} }} }}");
        project.Combine(".gitignore").WriteAllText($"Intermedia{Environment.NewLine}");

        new PackageRestoreService().Restore(project, new PackageRestoreOptions());
        // A second restore must not append the pattern again.
        new PackageRestoreService().Restore(project, new PackageRestoreOptions());

        var lines = project.Combine(".gitignore").ReadAllLines();
        Assert.That(lines.Count(line => line.Trim() == "/Packages/"), Is.EqualTo(1));
    }
}
