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
	/// The triplet a vcpkg dependency actually resolves to. An omitted <see cref="Triplet"/> means
	/// the host's, so the default has to be applied here rather than only at fetch time - otherwise
	/// <c>{ "vcpkg": "fmt" }</c> and <c>{ "vcpkg": "fmt", "triplet": "&lt;host&gt;" }</c> would look
	/// like conflicting pins on the very machine where they are identical, and the lock would record
	/// a pin that does not say which triplet was built.
	/// </summary>
	public string EffectiveTriplet =>
		string.IsNullOrWhiteSpace(Triplet) ? Fetchers.VcpkgPackageFetcher.DefaultTriplet() : Triplet;

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
			PackageSourceKind.Vcpkg => $"vcpkg:{Vcpkg}@{Version}:{EffectiveTriplet}",
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

/// <summary>
/// Names that come out of a manifest are attacker-controlled in the same sense the manifest is: a
/// package fetched from a remote declares its own transitive dependencies, and rbt turns those
/// names into filesystem paths and, for a binary package, into generated C# that the build then
/// compiles and runs. Both uses have to be gated.
/// </summary>
public static class PackageNames
{
	// Deliberately narrow. Anything outside this set has no legitimate use in a package name and
	// is exactly what a traversal ("../..") or an injection would need.
	private static readonly System.Text.RegularExpressions.Regex PackageNamePattern =
		new("^[A-Za-z0-9][A-Za-z0-9._-]*$", System.Text.RegularExpressions.RegexOptions.Compiled);

	// A generated rule declares "public class <name>", so the name has to be a plain C# identifier.
	private static readonly System.Text.RegularExpressions.Regex IdentifierPattern =
		new("^[A-Za-z_][A-Za-z0-9_]*$", System.Text.RegularExpressions.RegexOptions.Compiled);

	/// <summary>
	/// Validates a package name before it is combined into a path. Rejects separators, "." and
	/// ".." - a dependency keyed "../outside" would otherwise place the package next to
	/// <c>Packages/</c> rather than inside it.
	/// </summary>
	public static string ValidatePackageName(string name)
	{
		if (string.IsNullOrWhiteSpace(name) || !PackageNamePattern.IsMatch(name))
		{
			throw new PackageException(
				$"\"{name}\" is not a usable package name. Names may contain letters, digits, " +
				$"'.', '_' and '-', and must start with a letter or digit.");
		}
		return name;
	}

	/// <summary>
	/// Validates a name that will be emitted into generated C# source. Beyond path safety, a name
	/// carrying punctuation could close the class declaration and append arbitrary code to the
	/// rule assembly - which rbt compiles and executes as part of the build.
	/// </summary>
	public static string ValidateModuleName(string name, string packageName)
	{
		if (string.IsNullOrWhiteSpace(name) || !IdentifierPattern.IsMatch(name))
		{
			throw new PackageException(
				$"package \"{packageName}\" declares the module name \"{name}\", which is not a valid " +
				$"C# identifier. rbt generates a rule class from it, so it must be a plain identifier.");
		}
		return name;
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
