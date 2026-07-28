using NiceIO;
using Newtonsoft.Json;

namespace ReBuildTool.Service.PackageService;

/// <summary>
/// One resolved package as recorded in the lock file. <see cref="Resolved"/> is what makes a
/// restore reproducible: for git it is the commit a tag or branch actually pointed at, which
/// upstream is free to move afterwards.
/// </summary>
public class LockedPackage
{
	[JsonProperty("name")] public string Name { get; set; } = string.Empty;

	[JsonProperty("source")] public string Source { get; set; } = string.Empty;

	/// <summary>The git/http URL, or the declared path for a path dependency.</summary>
	[JsonProperty("origin")] public string? Origin { get; set; }

	/// <summary>
	/// Commit sha for git, archive sha256 for a URL, <c>port:triplet</c> for vcpkg. For a path
	/// dependency it is the path exactly as declared, not where it landed on this machine - an
	/// absolute path would make the committed lock useless to every other checkout.
	/// </summary>
	[JsonProperty("resolved")] public string? Resolved { get; set; }

	/// <summary>The pin this entry was produced from, so a changed manifest invalidates the lock.</summary>
	[JsonProperty("pin")] public string? Pin { get; set; }

	[JsonProperty("dependencies")] public List<string> Dependencies { get; set; } = new();
}

/// <summary>
/// <c>RBTPackage.lock.json</c> - the resolver's output, and the reason a second restore can skip
/// the network entirely. Meant to be committed to version control.
/// </summary>
public class PackageLockFile
{
	public const string FileName = "RBTPackage.lock.json";

	public const int CurrentVersion = 1;

	[JsonProperty("version")] public int Version { get; set; } = CurrentVersion;

	[JsonProperty("packages")] public List<LockedPackage> Packages { get; set; } = new();

	public static NPath PathIn(NPath projectRoot) => projectRoot.Combine(FileName);

	public static PackageLockFile? ReadFrom(NPath projectRoot)
	{
		var path = PathIn(projectRoot);
		if (!path.FileExists())
		{
			return null;
		}

		PackageLockFile? lockFile;
		try
		{
			lockFile = JsonConvert.DeserializeObject<PackageLockFile>(path.ReadAllText());
		}
		catch (JsonException e)
		{
			throw new PackageException($"{path} is not valid JSON: {e.Message}", e);
		}

		if (lockFile == null)
		{
			return null;
		}
		lockFile.Packages ??= new List<LockedPackage>();

		// A lock written by a newer rbt may use fields this build does not understand. Re-resolving
		// is always correct, so treat it as absent rather than failing the build.
		if (lockFile.Version != CurrentVersion)
		{
			return null;
		}
		return lockFile;
	}

	public LockedPackage? Find(string name)
	{
		return Packages.FirstOrDefault(package => package.Name == name);
	}

	/// <summary>
	/// Writes the lock only when its content actually changed. Rewriting it unconditionally would
	/// bump the file's timestamp on every single build, and rbt's incremental checks
	/// (<c>NeedReBuildRuleAssembly</c>, the makefile backend) are timestamp based - see the same
	/// reasoning in <c>CppModuleRule.GenerateCode</c>.
	/// </summary>
	public void WriteIfChanged(NPath projectRoot)
	{
		// Stable ordering keeps the file diff-friendly across machines.
		Packages = Packages.OrderBy(package => package.Name, StringComparer.Ordinal).ToList();
		var path = PathIn(projectRoot);
		var content = JsonConvert.SerializeObject(this, Formatting.Indented) + Environment.NewLine;
		if (path.FileExists() && path.ReadAllText() == content)
		{
			return;
		}
		path.EnsureParentDirectoryExists();
		path.WriteAllText(content);
	}
}
