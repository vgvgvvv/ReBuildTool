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
