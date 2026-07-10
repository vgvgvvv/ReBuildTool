using NiceIO;
using ReBuildTool.Service.CompileService;
using ResetCore.Common;

namespace ReBuildTool.ToolChain;

public static class RulePathUtility
{
	private static readonly object WarnLock = new();
	private static readonly HashSet<string> WarnedMissingPaths = new();

	public static IEnumerable<NPath> ExistingIncludePaths(
		IModuleInterface module,
		IEnumerable<string> includePaths,
		string source)
	{
		return ExistingModulePaths(module, includePaths, source, "include path", p => p.DirectoryExists());
	}

	public static IEnumerable<NPath> ExistingIncludePaths(
		IEnumerable<NPath> includePaths,
		string source)
	{
		return ExistingPaths(null, includePaths.Select(p => (Original: p.ToString(), Resolved: p)), source, "include path", p => p.DirectoryExists());
	}

	public static IEnumerable<NPath> ExistingSourceDirectories(
		IModuleInterface module,
		IEnumerable<string> sourceDirectories,
		string source)
	{
		return ExistingModulePaths(module, sourceDirectories, source, "source path", p => p.DirectoryExists());
	}

	public static IEnumerable<NPath> ExistingSourceFiles(
		IModuleInterface module,
		IEnumerable<string> sourceFiles,
		string source)
	{
		return ExistingModulePaths(module, sourceFiles, source, "source path", p => p.FileExists());
	}

	private static IEnumerable<NPath> ExistingModulePaths(
		IModuleInterface module,
		IEnumerable<string> paths,
		string source,
		string pathKind,
		Func<NPath, bool> exists)
	{
		var resolvedPaths = paths
			.Select(path => (Original: path, Resolved: ResolveModulePath(module, path)))
			.Where(path => path.Resolved is not null)
			.Select(path => (path.Original, Resolved: path.Resolved!));

		return ExistingPaths(module, resolvedPaths, source, pathKind, exists);
	}

	private static IEnumerable<NPath> ExistingPaths(
		IModuleInterface? module,
		IEnumerable<(string Original, NPath Resolved)> paths,
		string source,
		string pathKind,
		Func<NPath, bool> exists)
	{
		foreach (var (originalPath, resolvedPath) in paths)
		{
			if (!exists(resolvedPath))
			{
				WarnMissingPath(module, originalPath, resolvedPath, source, pathKind);
				continue;
			}

			yield return resolvedPath;
		}
	}

	private static NPath? ResolveModulePath(IModuleInterface module, string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return null;
		}

		if (Path.IsPathRooted(path) || string.IsNullOrEmpty(module.ModuleDirectory))
		{
			return path.ToNPath();
		}

		return module.ModuleDirectory.ToNPath().Combine(path);
	}

	private static void WarnMissingPath(
		IModuleInterface? module,
		string originalPath,
		NPath resolvedPath,
		string source,
		string pathKind)
	{
		var moduleName = module?.TargetName ?? "<global>";
		var warnKey = $"{pathKind}|{moduleName}|{source}|{resolvedPath}";
		lock (WarnLock)
		{
			if (!WarnedMissingPaths.Add(warnKey))
			{
				return;
			}
		}

		var moduleInfo = module == null ? string.Empty : $" for module {module.TargetName}";
		Log.Warning(
			$"Ignore missing {pathKind}{moduleInfo} from {source}: \"{originalPath}\" -> \"{resolvedPath}\"");
	}
}
