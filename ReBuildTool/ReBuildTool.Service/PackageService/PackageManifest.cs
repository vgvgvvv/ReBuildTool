using NiceIO;
using Newtonsoft.Json;

namespace ReBuildTool.Service.PackageService;

/// <summary>
/// Where a package's content comes from. Exactly one of the corresponding fields on
/// <see cref="PackageDependency"/> may be set.
/// </summary>
public enum PackageSourceKind
{
	Git,
	HttpArchive,
	Path,
	Vcpkg
}

/// <summary>
/// One entry of a manifest's <c>dependencies</c> map. All source fields are optional and
/// mutually exclusive - <see cref="ResolveKind"/> validates that exactly one is set.
/// </summary>
public class PackageDependency
{
	[JsonProperty("git")] public string? Git { get; set; }

	[JsonProperty("tag")] public string? Tag { get; set; }

	[JsonProperty("branch")] public string? Branch { get; set; }

	[JsonProperty("commit")] public string? Commit { get; set; }

	[JsonProperty("path")] public string? Path { get; set; }

	[JsonProperty("url")] public string? Url { get; set; }

	[JsonProperty("sha256")] public string? Sha256 { get; set; }

	/// <summary>
	/// Leading path components to strip when extracting an archive, like <c>tar --strip-components</c>.
	/// Upstream release tarballs almost always wrap everything in a single <c>name-version/</c>
	/// directory, so <c>1</c> is the common value.
	/// </summary>
	[JsonProperty("strip")] public int Strip { get; set; }

	[JsonProperty("vcpkg")] public string? Vcpkg { get; set; }

	/// <summary>
	/// vcpkg triplet, e.g. <c>x64-windows</c>. Defaults to the host's. Restore runs before any
	/// build context exists, so a cross-compiled build has to name the triplet explicitly.
	/// </summary>
	[JsonProperty("triplet")] public string? Triplet { get; set; }

	[JsonProperty("version")] public string? Version { get; set; }

	/// <summary>
	/// Path (relative to the manifest that declares this dependency) of a <c>.module.cs</c> to
	/// copy into the fetched package. For upstream sources that ship no rbt rule of their own.
	/// </summary>
	[JsonProperty("overlay")] public string? Overlay { get; set; }

	public PackageSourceKind ResolveKind(string packageName)
	{
		var kinds = new List<PackageSourceKind>();
		if (!string.IsNullOrWhiteSpace(Git))
		{
			kinds.Add(PackageSourceKind.Git);
		}
		if (!string.IsNullOrWhiteSpace(Url))
		{
			kinds.Add(PackageSourceKind.HttpArchive);
		}
		if (!string.IsNullOrWhiteSpace(Path))
		{
			kinds.Add(PackageSourceKind.Path);
		}
		if (!string.IsNullOrWhiteSpace(Vcpkg))
		{
			kinds.Add(PackageSourceKind.Vcpkg);
		}

		if (kinds.Count == 0)
		{
			throw new PackageException(
				$"package \"{packageName}\" declares no source: set exactly one of " +
				$"\"git\", \"url\", \"path\" or \"vcpkg\".");
		}
		if (kinds.Count > 1)
		{
			throw new PackageException(
				$"package \"{packageName}\" declares more than one source ({string.Join(", ", kinds)}): " +
				$"set exactly one of \"git\", \"url\", \"path\" or \"vcpkg\".");
		}

		if (kinds[0] == PackageSourceKind.Git && Commit == null && Tag == null && Branch == null)
		{
			throw new PackageException(
				$"package \"{packageName}\" pins no git revision: set \"commit\", \"tag\" or \"branch\". " +
				$"rbt resolves exact pins only - it never picks a version for you.");
		}

		return kinds[0];
	}

	/// <summary>
	/// The git revision to check out, most specific first. A commit is reproducible, a tag can be
	/// moved upstream, a branch moves constantly - but the lock file always records the commit
	/// each of them actually resolved to.
	/// </summary>
	public string? GitRevision => Commit ?? Tag ?? Branch;

	/// <summary>
	/// Identity of this pin, used to detect conflicting declarations of the same package name
	/// coming from different manifests. Two dependencies with the same key are interchangeable.
	/// </summary>
	public string PinKey(string packageName)
	{
		return ResolveKind(packageName) switch
		{
			PackageSourceKind.Git => $"git:{Git}@{GitRevision}",
			PackageSourceKind.HttpArchive => $"url:{Url}@{Sha256}",
			PackageSourceKind.Path => $"path:{Path}",
			PackageSourceKind.Vcpkg => $"vcpkg:{Vcpkg}@{Version}:{Triplet}",
			_ => throw new PackageException($"unknown source kind for package \"{packageName}\"")
		};
	}

	public string Describe(string packageName) => PinKey(packageName);
}

/// <summary>
/// Prebuilt artifacts shipped by a binary package, declared in the package's own manifest.
/// Platform / architecture / configuration are matched as strings because the enums that
/// name them (<c>PlatformSupportType</c>, <c>BuildConfiguration</c>, <c>Architecture</c>)
/// live in ReBuildTool.CppCompiler, which this assembly sits below.
/// </summary>
public class PackageBinarySpec
{
	/// <summary>
	/// Name of the module rbt synthesizes for these artifacts, and therefore the name consumers
	/// put in <c>Dependencies</c>. Defaults to the package name.
	/// </summary>
	[JsonProperty("module")] public string? Module { get; set; }

	[JsonProperty("includes")] public List<string> Includes { get; set; } = new();

	[JsonProperty("artifacts")] public List<PackageBinaryArtifact> Artifacts { get; set; } = new();
}

public class PackageBinaryArtifact
{
	/// <summary>Matches <c>PlatformSupportType</c> by name (Windows, Linux, MacOSX, ...). Null matches every platform.</summary>
	[JsonProperty("platform")] public string? Platform { get; set; }

	/// <summary>Matches <c>Architecture.CommandLineName</c> (x86, x64, arm32, arm64). Null matches every architecture.</summary>
	[JsonProperty("arch")] public string? Arch { get; set; }

	/// <summary>Matches <c>BuildConfiguration</c> by name (Debug, Release, ...). Null matches every configuration.</summary>
	[JsonProperty("config")] public string? Config { get; set; }

	[JsonProperty("libraryDirectories")] public List<string> LibraryDirectories { get; set; } = new();

	[JsonProperty("staticLibraries")] public List<string> StaticLibraries { get; set; } = new();

	[JsonProperty("dynamicLibraries")] public List<string> DynamicLibraries { get; set; } = new();

	[JsonProperty("defines")] public List<string> Defines { get; set; } = new();
}

/// <summary>
/// A project's or a package's <c>RBTPackage.json</c>. A package declares its own transitive
/// dependencies with the very same file, which is what lets the resolver walk the graph.
/// </summary>
public class PackageManifest
{
	public const string FileName = "RBTPackage.json";

	[JsonProperty("name")] public string? Name { get; set; }

	[JsonProperty("dependencies")] public Dictionary<string, PackageDependency> Dependencies { get; set; } = new();

	/// <summary>
	/// Root-manifest-only escape hatch: when two packages pin the same dependency differently the
	/// resolver refuses to guess, and the user names the winning pin here.
	/// </summary>
	[JsonProperty("overrides")] public Dictionary<string, PackageDependency> Overrides { get; set; } = new();

	[JsonProperty("binary")] public PackageBinarySpec? Binary { get; set; }

	public static NPath PathIn(NPath directory) => directory.Combine(FileName);

	/// <summary>
	/// Reads the manifest sitting in <paramref name="directory"/>, or null when there is none -
	/// a package without a manifest is legal, it simply has no transitive dependencies.
	/// </summary>
	public static PackageManifest? ReadFrom(NPath directory)
	{
		var path = PathIn(directory);
		if (!path.FileExists())
		{
			return null;
		}
		return Parse(path.ReadAllText(), path);
	}

	public static PackageManifest Parse(string json, NPath origin)
	{
		PackageManifest? manifest;
		try
		{
			manifest = JsonConvert.DeserializeObject<PackageManifest>(json);
		}
		catch (JsonException e)
		{
			throw new PackageException($"{origin} is not valid JSON: {e.Message}", e);
		}

		if (manifest == null)
		{
			throw new PackageException($"{origin} is empty.");
		}

		// A "dependencies": null in the file deserializes to null rather than the initializer.
		manifest.Dependencies ??= new Dictionary<string, PackageDependency>();
		manifest.Overrides ??= new Dictionary<string, PackageDependency>();
		return manifest;
	}
}

public class PackageException : Exception
{
	public PackageException(string message) : base(message)
	{
	}

	public PackageException(string message, Exception inner) : base(message, inner)
	{
	}
}
