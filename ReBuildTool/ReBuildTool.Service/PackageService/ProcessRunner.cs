using System.Diagnostics;
using System.Text;
using NiceIO;

namespace ReBuildTool.Service.PackageService;

public class ProcessResult
{
	public int ExitCode { get; init; }

	public string StdOut { get; init; } = string.Empty;

	public string StdErr { get; init; } = string.Empty;

	public bool IsSuccess => ExitCode == 0;
}

/// <summary>
/// Runs an external tool and captures its output.
///
/// The package layer cannot use <c>ReBuildTool.Service.Global.Shell</c> for this: that wrapper
/// forwards stdout/stderr straight into the logger and keeps nothing, while resolving a git pin
/// means reading <c>git rev-parse HEAD</c> back. Arguments go through <c>ArgumentList</c> so the
/// runtime applies the OS-correct argv quoting per token, exactly as Shell does.
/// </summary>
internal static class ProcessRunner
{
	public static ProcessResult Run(string program, IEnumerable<string> arguments, NPath? workingDirectory = null)
	{
		var startInfo = new ProcessStartInfo
		{
			FileName = program,
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			CreateNoWindow = true
		};
		foreach (var argument in arguments)
		{
			startInfo.ArgumentList.Add(argument);
		}
		if (workingDirectory != null && workingDirectory.DirectoryExists())
		{
			startInfo.WorkingDirectory = workingDirectory.ToString();
		}

		using var process = new Process();
		process.StartInfo = startInfo;

		var stdOut = new StringBuilder();
		var stdErr = new StringBuilder();
		process.OutputDataReceived += (_, args) =>
		{
			if (args.Data != null)
			{
				stdOut.AppendLine(args.Data);
			}
		};
		process.ErrorDataReceived += (_, args) =>
		{
			if (args.Data != null)
			{
				stdErr.AppendLine(args.Data);
			}
		};

		try
		{
			process.Start();
		}
		catch (Exception e)
		{
			throw new PackageException($"cannot run \"{program}\": {e.Message}", e);
		}

		process.BeginOutputReadLine();
		process.BeginErrorReadLine();
		process.WaitForExit();

		return new ProcessResult
		{
			ExitCode = process.ExitCode,
			StdOut = stdOut.ToString(),
			StdErr = stdErr.ToString()
		};
	}

	/// <summary>Runs the tool and returns its trimmed stdout, throwing with the captured stderr on failure.</summary>
	public static string RunOrThrow(
		string program,
		IEnumerable<string> arguments,
		NPath? workingDirectory,
		string what)
	{
		var argumentList = arguments.ToList();
		var result = Run(program, argumentList, workingDirectory);
		if (!result.IsSuccess)
		{
			var details = string.IsNullOrWhiteSpace(result.StdErr) ? result.StdOut : result.StdErr;
			throw new PackageException(
				$"{what} failed (exit {result.ExitCode}): {program} {string.Join(" ", argumentList)}{Environment.NewLine}{details.Trim()}");
		}
		return result.StdOut.Trim();
	}
}
