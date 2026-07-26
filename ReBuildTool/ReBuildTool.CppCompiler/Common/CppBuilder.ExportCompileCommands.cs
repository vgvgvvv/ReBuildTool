using NiceIO;
using ReBuildTool.Service.CompileService;

namespace ReBuildTool.ToolChain;

/// <summary>
/// A single entry of a JSON Compilation Database (compile_commands.json). Fields map
/// 1:1 to the LLVM spec (https://clang.llvm.org/docs/JSONCompilationDatabase.html):
/// clangd / VS Code / CLion read these to power syntax highlighting and go-to-definition.
/// </summary>
public class CompileCommandEntry
{
	// The working directory the compile is run from (used to resolve relative paths).
	public string Directory { get; set; } = string.Empty;

	// The source file being compiled.
	public string File { get; set; } = string.Empty;

	// The full argv (argv[0] is the compiler executable). Preferred over "command"
	// because it needs no shell-unescaping.
	public List<string> Arguments { get; set; } = new();

	// The object file this compile produces.
	public string Output { get; set; } = string.Empty;
}

public partial class CppBuilder
{
	/// <summary>
	/// Collects the exact per-source-file compiler invocations rbt would run, as
	/// <see cref="CompileCommandEntry"/> records - without compiling anything. Mirrors the
	/// module pending/dependency walk in <see cref="BuildPendingModules"/> so every module
	/// used by any target (and its dependencies) is set up before its flags are read.
	/// A source shared across targets is emitted once (keyed by file path).
	/// </summary>
	public List<CompileCommandEntry> CollectCompileCommands()
	{
		var entries = new List<CompileCommandEntry>();
		var seenFiles = new HashSet<string>();

		foreach (var targetRule in CurrentSource.TargetRules.Values)
		{
			PendingTargetRule(targetRule);
		}

		while (PendingModulesQueue.Count > 0)
		{
			var module = PendingModulesQueue.Dequeue();
			var process = CompileProcess.Create(module, this);
			foreach (var entry in process.CollectCompileCommandEntries())
			{
				if (seenFiles.Add(entry.File))
				{
					entries.Add(entry);
				}
			}
		}

		return entries;
	}

	/// <summary>
	/// Generates a <c>build.ninja</c> for every module reachable from the current targets (the
	/// same per-module file rbt's Ninja backend writes), without executing ninja. Mirrors
	/// <see cref="CollectCompileCommands"/>: it walks the pending/dependency queue so every module
	/// used by any target is set up before its file is generated.
	/// </summary>
	public List<NPath> GenerateNinjaFiles()
	{
		var paths = new List<NPath>();
		foreach (var targetRule in CurrentSource.TargetRules.Values)
		{
			PendingTargetRule(targetRule);
		}

		while (PendingModulesQueue.Count > 0)
		{
			var module = PendingModulesQueue.Dequeue();
			var process = CompileProcess.Create(module, this);
			paths.Add(process.GenerateNinjaFile());
		}

		return paths;
	}

	/// <summary>
	/// Generates a <c>Makefile</c> for every module reachable from the current targets (the
	/// same per-module file rbt's Makefile backend writes), without running make. Mirrors
	/// <see cref="CollectCompileCommands"/>.
	/// </summary>
	public List<NPath> GenerateMakeFiles()
	{
		var paths = new List<NPath>();
		foreach (var targetRule in CurrentSource.TargetRules.Values)
		{
			PendingTargetRule(targetRule);
		}

		while (PendingModulesQueue.Count > 0)
		{
			var module = PendingModulesQueue.Dequeue();
			var process = CompileProcess.Create(module, this);
			paths.Add(process.GenerateMakeFile());
		}

		return paths;
	}

	public partial class CompileProcess
	{
		/// <summary>
		/// Reuses the same source discovery + per-file argument construction as a real
		/// compile (<see cref="CollectCompileUnit"/> / <see cref="CollectCompileInvocations"/>),
		/// but yields the invocations as compilation-database entries instead of running them.
		/// </summary>
		public IEnumerable<CompileCommandEntry> CollectCompileCommandEntries()
		{
			if (!CollectCompileUnit())
			{
				yield break;
			}

			if (!CollectCompileInvocations())
			{
				yield break;
			}

			foreach (var invocation in CompileInvocation)
			{
				// Argument tokens are now clean argv (the toolchains emit unquoted paths; quoting
				// is applied per consumer — Shell's ArgumentList, NinjaFileGenerator.NinjaVar,
				// ShellQuote for makefile recipes). The compile_commands.json `arguments` array
				// wants exactly those clean tokens (LLVM JSON Compilation Database spec), with
				// the JSON serializer supplying its own quoting. StripEmbeddedQuotes is kept as
				// a defensive safety net in case a future toolchain reintroduces embedded quotes.
				var arguments = new List<string>(invocation.Arguments.Count + 1) { invocation.ProgramName };
				arguments.AddRange(invocation.Arguments.Select(StripEmbeddedQuotes));

				yield return new CompileCommandEntry
				{
					Directory = Source.ProjectRoot.ToString(),
					File = invocation.Unit.SourceFile.ToString(),
					Arguments = arguments,
					Output = invocation.Unit.OutputFile.ToString(),
				};
			}
		}

		/// <summary>
		/// Defensive safety net: strips any shell-level double quotes that may have leaked into
		/// an argument token. The toolchains now emit clean unquoted argv tokens (quoting is
		/// applied per consumer instead), so this is normally a no-op. It is retained to keep
		/// the <c>compile_commands.json</c> <c>arguments</c> array robust if a toolchain ever
		/// reintroduces embedded quotes. Only a wrapping quote pair is stripped — quotes that
		/// are genuinely part of a value (e.g. a -D macro body) are left untouched. Handles:
		///   - flag+quoted-value:  `-I"D:\a b"` / `/I"D:\a b"` / `/Fo"D:\a b"`
		///   - key=quoted-value:   `--sysroot="D:\a b"`
		///   - standalone quoted:  `"D:\a b"`
		/// </summary>
		private static string StripEmbeddedQuotes(string arg)
		{
			if (string.IsNullOrEmpty(arg) || arg.Length < 2)
			{
				return arg;
			}

			// key=value form (e.g. --sysroot="..."): only strip quotes after the '='.
			var eq = arg.IndexOf('=');
			if (eq >= 0 && eq < arg.Length - 1)
			{
				var head = arg.Substring(0, eq + 1);
				var tail = arg.Substring(eq + 1);
				return head + Unquote(tail);
			}

			return Unquote(arg);

			static string Unquote(string s)
			{
				// Invert the quoting patterns the toolchains emit. The wrapping pair can sit
				// at the very ends (`"D:\a b"`) or behind a flag prefix (`-I"D:\a b"`,
				// `/Fo"D:\a b"`). If the token ends with a quote and contains an opening
				// quote after the leading flag chars, drop that pair. This never touches a
				// value whose quotes are part of the data (e.g. a -D macro body).
				if (s.Length < 2 || s[^1] != '"')
				{
					return s;
				}
				var open = s.IndexOf('"');
				// Only the trailing quote is present (e.g. `D:\a"`): leave it alone.
				if (open < 0 || open == s.Length - 1)
				{
					return s;
				}
				return s.Remove(open, 1).Remove(s.Length - 2, 1);
			}
		}
	}
}
