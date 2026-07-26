using NiceIO;

namespace ReBuildTool.ToolChain;

public partial class CppBuilder
{
	/// <summary>
	/// Walks the module pending/dependency queue the same way <see cref="BuildPendingModules"/>
	/// does and yields a <see cref="CompileProcess"/> per reachable module, so every module used
	/// by any target (and its dependencies) is set up before its flags are read. Shared by the
	/// three "produce something without building it" entry points: compile_commands.json,
	/// build.ninja and Makefile generation.
	/// </summary>
	private IEnumerable<CompileProcess> EnumerateModuleProcesses()
	{
		foreach (var targetRule in CurrentSource.TargetRules.Values)
		{
			PendingTargetRule(targetRule);
		}

		while (PendingModulesQueue.Count > 0)
		{
			var module = PendingModulesQueue.Dequeue();
			yield return CompileProcess.Create(module, this);
		}
	}

	/// <summary>
	/// Generates a <c>build.ninja</c> for every module reachable from the current targets (the
	/// same per-module file rbt's Ninja backend writes), without executing ninja.
	/// </summary>
	public List<NPath> GenerateNinjaFiles()
	{
		return EnumerateModuleProcesses().Select(process => process.GenerateNinjaFile()).ToList();
	}

	/// <summary>
	/// Generates a <c>Makefile</c> for every module reachable from the current targets (the
	/// same per-module file rbt's Makefile backend writes), without running make.
	/// </summary>
	public List<NPath> GenerateMakeFiles()
	{
		return EnumerateModuleProcesses().Select(process => process.GenerateMakeFile()).ToList();
	}
}
