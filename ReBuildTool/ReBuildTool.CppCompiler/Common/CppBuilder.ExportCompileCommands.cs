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
	/// <see cref="CompileCommandEntry"/> records - without compiling anything. Walks the
	/// modules via <see cref="EnumerateModuleProcesses"/> so every module used by any target
	/// (and its dependencies) is set up before its flags are read. A source shared across
	/// targets is emitted once (keyed by file path).
	/// </summary>
	public List<CompileCommandEntry> CollectCompileCommands()
	{
		var entries = new List<CompileCommandEntry>();
		var seenFiles = new HashSet<string>();

		foreach (var process in EnumerateModuleProcesses())
		{
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
				// Argument tokens are clean argv (the toolchains emit unquoted paths; quoting is
				// applied per consumer — Shell's ArgumentList, ShellQuote for ninja/makefile
				// command lines). The compile_commands.json `arguments` array wants exactly
				// those tokens (LLVM JSON Compilation Database spec), with the JSON serializer
				// supplying its own escaping — so they are passed through untouched. Nothing
				// strips quotes here on purpose: a quote that survives to this point belongs to
				// the data (e.g. a -DNAME="a b" macro body) and removing it would change what
				// the indexer thinks the macro expands to.
				var arguments = new List<string>(invocation.Arguments.Count + 1) { invocation.ProgramName };
				arguments.AddRange(invocation.Arguments);

				yield return new CompileCommandEntry
				{
					Directory = Source.ProjectRoot.ToString(),
					File = invocation.Unit.SourceFile.ToString(),
					Arguments = arguments,
					Output = invocation.Unit.OutputFile.ToString(),
				};
			}
		}
	}
}
