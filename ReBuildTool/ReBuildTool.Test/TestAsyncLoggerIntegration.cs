using System.IO;
using System.Threading;
using NUnit.Framework;
using ResetCore.Common;

namespace ReBuildTool.Test;

/// <summary>
/// End-to-end verification that the production assembly path
/// (<c>Console + File</c> via <see cref="Log.AppendLogger"/>, then
/// <see cref="Log.EnableAsync"/>) actually engages the async wrapper and that
/// concurrent logging lands safely, completely, and in order. This mirrors what
/// both Program.cs entry points do at startup.
/// </summary>
[TestFixture]
public class TestAsyncLoggerIntegration
{
    private static string NewTempLogFile()
    {
        // FileLogger.AppendText() creates the file; start from empty each time.
        var path = Path.GetTempFileName();
        File.Delete(path);
        return path;
    }

    /// <summary>
    /// Reads all lines from a file even while another handle (the FileLogger's
    /// StreamWriter) still holds it open for writing. Plain <c>File.ReadAllLines</c>
    /// opens with <c>FileShare.Read</c>, which conflicts with the live write
    /// handle on Windows and throws. <c>FileShare.ReadWrite</c> coexists with it.
    /// </summary>
    private static List<string> ReadAllLinesShared(string path)
    {
        var lines = new List<string>();
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(fs);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            lines.Add(line);
        }
        return lines;
    }

    /// <summary>
    /// FileLogger only closes its StreamWriter in a finalizer, so the OS file
    /// handle stays live until GC. Force finalization so we can reliably delete
    /// the temp file; if it still fails (timing), give up rather than mask the
    /// real test result with a cleanup exception.
    /// </summary>
    private static void TryDelete(string path)
    {
        try
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
            // Best-effort cleanup only; never let a temp-file delete failure
            // mask the actual assertion outcome.
        }
    }

    private static void ResetLogger()
    {
        // Restore a plain synchronous console logger so one test's async wrapper
        // (now shut down) does not leak into the next test.
        Log.SetLogger(new ConsoleLogger().WithDate());
    }

    [TearDown]
    public void TearDown()
    {
        ResetLogger();
    }

    [Test]
    public void EnableAsync_ReplacesLoggerWithAsyncQueueLogger()
    {
        // Reproduce the Program.cs assembly sequence exactly.
        Log.SetLogger(new ConsoleLogger().WithDate());
        Log.AppendLogger(new FileLogger(NewTempLogFile()).WithDate());

        var wrapper = Log.EnableAsync();

        Assert.That(wrapper, Is.Not.Null, "EnableAsync must return the wrapper");
        Assert.That(Log.Logger, Is.TypeOf<AsyncQueueLogger>(),
            "After EnableAsync the active logger must be the async wrapper.");

        Log.Shutdown();

        // Idempotency: a second EnableAsync must reuse the existing wrapper
        // rather than stacking a second queue/thread.
        var wrapper2 = Log.EnableAsync();
        Assert.That(wrapper2, Is.SameAs(wrapper),
            "EnableAsync should be idempotent and reuse the existing wrapper.");
        Log.Shutdown();
    }

    [Test]
    public void SingleThread_Submits_AreDrainedInFifoOrder()
    {
        var file = NewTempLogFile();
        try
        {
            // File-only (no console) so thousands of lines don't spam the test
            // output. We're verifying drain ordering here, not console assembly.
            Log.SetLogger(new FileLogger(file).WithDate());
            Log.EnableAsync();

            const int total = 2000;
            for (int i = 0; i < total; i++)
            {
                Log.Info($"fifo-{i}");
            }
            Log.Shutdown();

            var lines = ReadAllLinesShared(file);
            Assert.That(lines.Count, Is.EqualTo(total),
                "Every submitted line must reach the file exactly once.");

            // Strict FIFO: line i must end with the marker submitted at step i.
            for (int i = 0; i < total; i++)
            {
                StringAssert.EndsWith($"fifo-{i}", lines[i],
                    $"Line {i} must preserve submit order (expected fifo-{i}).");
            }
        }
        finally
        {
            TryDelete(file);
        }
    }

    [Test]
    public void MultiThread_Submits_AreSafe_Complete_AndUncorrupted()
    {
        var file = NewTempLogFile();
        try
        {
            Log.SetLogger(new FileLogger(file).WithDate());
            Log.EnableAsync();

            const int total = 5000;
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 8 };
            // Hammer Log.Info from 8 threads, each claiming a unique id — same
            // shape as the Parallel.ForEach compile loop in CppBuilder.
            int next = -1;
            Parallel.For(0, total, parallelOptions, _ =>
            {
                int id = Interlocked.Increment(ref next);
                Log.Info($"mt-{id}");
            });
            Log.Shutdown();

            var lines = ReadAllLinesShared(file);

            // No line dropped or doubled (this is what used to throw or corrupt
            // when FileLogger's StreamWriter was hit concurrently).
            Assert.That(lines.Count, Is.EqualTo(total),
                "Concurrent submission must not drop or duplicate lines.");

            // Every line must be structurally complete: the [Info] tag must be
            // intact on every line, proving the per-line color/text calls were
            // never split by interleaving.
            foreach (var line in lines)
            {
                StringAssert.Contains("[Info]", line);
            }

            // Every submitted id must parse back exactly once: proves no loss,
            // no dup, and no line got chopped so badly its trailing id broke.
            var seen = new HashSet<int>(total);
            int parsed = 0;
            const string tag = "mt-";
            foreach (var line in lines)
            {
                var idx = line.LastIndexOf(tag, StringComparison.Ordinal);
                if (idx < 0) continue;
                if (int.TryParse(line.AsSpan(idx + tag.Length), out var id))
                {
                    seen.Add(id);
                    parsed++;
                }
            }
            Assert.That(parsed, Is.EqualTo(total),
                "Every file line must parse back to its submitted id.");
            Assert.That(seen.Count, Is.EqualTo(total),
                "Submitted ids must be a permutation of 0..N-1.");
        }
        finally
        {
            TryDelete(file);
        }
    }

    /// <summary>
    /// Negative-path A/B control: drives the <b>same</b> concurrent load as
    /// <see cref="MultiThread_Submits_AreSafe_Complete_AndUncorrupted"/> but
    /// <b>without</b> <see cref="Log.EnableAsync"/>. With the raw FileLogger
    /// wired directly, its single <c>StreamWriter</c> is hit from multiple
    /// threads. Modern .NET StreamWriters do not throw on concurrent use — they
    /// <b>silently corrupt</b>: concurrent WriteLine calls overwrite each other
    /// mid-buffer and the file ends up with fewer lines than were submitted.
    /// <para>
    /// This is the proof that the green tests above are green <em>because</em>
    /// the async wrapper is doing its job — not because the test happens to be
    /// harmless. If this test ever reliably passes (no lines lost across runs),
    /// the async wrapper is no longer the load-bearing piece.
    /// </para><para>
    /// The amount of loss is non-deterministic (depends on the OS scheduler),
    /// so the test is marked <see cref="ExplicitAttribute"/> to avoid flaking
    /// the suite in CI. Run it on demand to confirm the baseline behavior.
    /// </para>
    /// </summary>
    [Test]
    [Explicit("Non-deterministic: line loss depends on the OS scheduler. Run on demand.")]
    public void WithoutEnableAsync_ConcurrentFileLogger_LosesLines()
    {
        var file = NewTempLogFile();
        try
        {
            // Direct FileLogger — no async wrapper. This is the pre-fix state.
            Log.SetLogger(new FileLogger(file).WithDate());

            const int total = 20_000;
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 8 };
            int next = -1;
            Parallel.For(0, total, parallelOptions, _ =>
            {
                int id = Interlocked.Increment(ref next);
                Log.Info($"mt-{id}");
            });

            // No Log.Shutdown() here — there is no async queue to drain. The
            // StreamWriter.AutoFlush=true on FileLogger writes each line inline.
            // Give the OS a moment to settle, then read.
            Thread.Sleep(200);
            var lines = ReadAllLinesShared(file);

            // With the raw (non-thread-safe) StreamWriter hit from 8 threads,
            // a fraction of lines silently overwrite each other and never make
            // it to disk. We assert that some loss occurred, which is exactly
            // the bug the async wrapper exists to fix.
            Assert.That(lines.Count, Is.LessThan(total),
                $"Expected the raw FileLogger to LOSE lines under concurrency, " +
                $"but got {lines.Count}/{total}. If the wrapper is no longer " +
                $"load-bearing this assertion would catch it — but more likely " +
                $"the scheduler just didn't interleave this run; try again or " +
                $"raise the load.");
        }
        finally
        {
            TryDelete(file);
        }
    }

    /// <summary>
    /// A sink that records the managed thread id of the first write it performs,
    /// so we can prove the write happened on the async background thread, not
    /// the caller's thread. This is the most direct evidence that "async" is
    /// actually engaged (rather than the wrapper silently synchronously
    /// dispatching).
    /// </summary>
    private class ThreadRecordingLogger : ILogger
    {
        public LogLevel Level { get; set; } = LogLevel.Info;
        public bool Enable { get; set; } = true;
        public int? WriterThreadId;

        public void Info(params object[] info)
        {
            WriterThreadId ??= Thread.CurrentThread.ManagedThreadId;
        }

        public void Debug(params object[] info) { }
        public void Warning(params object[] info) { }
        public void Error(params object[] info) { }
        public void Exception(Exception ex) { }
    }

    [Test]
    public void Writes_HappenOnBackgroundThread_NotCaller()
    {
        var sink = new ThreadRecordingLogger();
        Log.SetLogger(sink);
        Log.EnableAsync();

        int callerThreadId = Thread.CurrentThread.ManagedThreadId;
        Log.Info("ping");

        // Shutdown joins the writer thread, so once it returns the write has
        // either completed or never happened.
        Log.Shutdown();

        Assert.That(sink.WriterThreadId, Is.Not.Null,
            "The sink must have been invoked at all.");
        Assert.That(sink.WriterThreadId, Is.Not.EqualTo(callerThreadId),
            "The write must run on the background writer thread, proving async dispatch is live.");
    }
}
