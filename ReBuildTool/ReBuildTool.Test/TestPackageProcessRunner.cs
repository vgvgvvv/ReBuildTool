using NiceIO;
using ReBuildTool.Service.PackageService;

namespace ReBuildTool.Test;

/// <summary>
/// The external-tool runner the package fetchers shell out through.
/// </summary>
[TestFixture]
public class TestPackageProcessRunner
{
    private NPath WorkDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        WorkDirectory = Path.Combine(Path.GetTempPath(), $"rbt-runner-{Guid.NewGuid():N}")
            .ToNPath()
            .EnsureDirectoryExists();
    }

    [TearDown]
    public void TearDown()
    {
        WorkDirectory.DeleteIfExists(DeleteMode.Normal);
    }

    /// <summary>
    /// Silently skipping a missing working directory would run the tool in rbt's own working
    /// directory instead - a git command against the wrong repository, and a failure with no hint
    /// as to why. A directory the caller named has to be there.
    /// </summary>
    [Test]
    public void AMissingWorkingDirectoryIsAnError()
    {
        var missing = WorkDirectory.Combine("not-here");

        var exception = Assert.Throws<PackageException>(
            () => ProcessRunner.Run("git", new[] { "--version" }, missing));

        Assert.That(exception!.Message, Does.Contain("not-here"));
        Assert.That(exception.Message, Does.Contain("does not exist"));
    }

    [Test]
    public void ANullWorkingDirectoryIsFine()
    {
        // Cloning happens before the destination exists, so "no directory" stays legal.
        var result = ProcessRunner.Run("git", new[] { "--version" });

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.StdOut, Does.Contain("git version"));
    }

    [Test]
    public void StdOutIsCapturedInFull()
    {
        var result = ProcessRunner.Run("git", new[] { "--version" }, WorkDirectory);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.StdOut.Trim(), Is.Not.Empty);
    }

    /// <summary>A failure has to carry the tool's own diagnostics, or RunOrThrow reports nothing useful.</summary>
    [Test]
    public void AFailureCarriesTheToolsMessage()
    {
        var exception = Assert.Throws<PackageException>(() => ProcessRunner.RunOrThrow(
            "git",
            new[] { "rev-parse", "--verify", "definitely-not-a-ref" },
            WorkDirectory,
            "resolving a revision"));

        Assert.That(exception!.Message, Does.Contain("resolving a revision"));
        // Non-empty tail: the exact wording is git's, but something has to come back.
        Assert.That(exception.Message.Length, Is.GreaterThan("resolving a revision".Length + 20));
    }

    [Test]
    public void AMissingProgramIsReportedByName()
    {
        var exception = Assert.Throws<PackageException>(
            () => ProcessRunner.Run("rbt-no-such-tool", Array.Empty<string>()));

        Assert.That(exception!.Message, Does.Contain("rbt-no-such-tool"));
    }
}
