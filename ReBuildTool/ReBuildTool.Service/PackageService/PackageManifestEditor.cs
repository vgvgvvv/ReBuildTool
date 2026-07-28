using NiceIO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ResetCore.Common;

namespace ReBuildTool.Service.PackageService;

/// <summary>
/// Adds and removes dependencies in a project's <c>RBTPackage.json</c> from the command line.
///
/// Edits go through <see cref="JObject"/> rather than a round trip of the typed manifest so that
/// anything rbt does not model - comments aside, any field a newer rbt or a human added - survives
/// the edit instead of being silently dropped.
/// </summary>
public static class PackageManifestEditor
{
	/// <summary>
	/// Parses the compact spec accepted by <c>--PackageAdd</c>. The qualifier after '#' is required
	/// only for git, which has no default revision to fall back on:
	/// <list type="bullet">
	/// <item><c>git:&lt;url&gt;#&lt;tag-or-commit&gt;</c></item>
	/// <item><c>path:&lt;dir&gt;</c></item>
	/// <item><c>url:&lt;href&gt;[#&lt;sha256&gt;]</c> - without a checksum the archive is not verified</item>
	/// <item><c>vcpkg:&lt;port&gt;[#&lt;triplet&gt;]</c> - the triplet defaults to the host's</item>
	/// </list>
	/// </summary>
	public static PackageDependency ParseSpec(string spec)
	{
		var separator = spec.IndexOf(':');
		if (separator <= 0)
		{
			throw new PackageException(
				$"cannot read package spec \"{spec}\": expected one of " +
				$"git:<url>#<tag-or-commit>, path:<dir>, url:<href>[#<sha256>], " +
				$"vcpkg:<port>[#<triplet>].");
		}

		var kind = spec.Substring(0, separator).ToLowerInvariant();
		var rest = spec.Substring(separator + 1);

		// Split on the LAST '#': a URL may legitimately contain one, the qualifier never does.
		string? qualifier = null;
		var hash = rest.LastIndexOf('#');
		if (hash >= 0)
		{
			qualifier = rest.Substring(hash + 1);
			rest = rest.Substring(0, hash);
		}

		switch (kind)
		{
			case "git":
				if (qualifier == null)
				{
					throw new PackageException(
						$"git spec \"{spec}\" pins no revision: write git:<url>#<tag-or-commit>. " +
						$"rbt resolves exact pins only.");
				}
				// A 40-character hex string is a commit; anything else is a tag. Guessing wrong is
				// harmless - both resolve to a commit in the lock - but this keeps the manifest honest.
				return IsCommitSha(qualifier)
					? new PackageDependency { Git = rest, Commit = qualifier }
					: new PackageDependency { Git = rest, Tag = qualifier };
			case "path":
				return new PackageDependency { Path = rest };
			case "url":
				return new PackageDependency { Url = rest, Sha256 = qualifier };
			case "vcpkg":
				return new PackageDependency { Vcpkg = rest, Triplet = qualifier };
			default:
				throw new PackageException(
					$"unknown package source \"{kind}\" in \"{spec}\": expected git, path, url or vcpkg.");
		}
	}

	private static bool IsCommitSha(string value)
	{
		return value.Length == 40 && value.All(Uri.IsHexDigit);
	}

	/// <summary>Adds or replaces one dependency. Returns true when the file changed.</summary>
	public static bool Add(NPath projectRoot, string entry)
	{
		var separator = entry.IndexOf('=');
		if (separator <= 0)
		{
			throw new PackageException(
				$"cannot read --PackageAdd \"{entry}\": expected <Name>=<source spec>, " +
				$"for example MyLib=git:https://github.com/x/y.git#v1.0.");
		}

		var name = entry.Substring(0, separator).Trim();
		var dependency = ParseSpec(entry.Substring(separator + 1).Trim());
		// Validate before writing: a manifest that cannot be resolved is worse than a rejected edit.
		dependency.ResolveKind(name);

		var root = Load(projectRoot);
		var dependencies = root["dependencies"] as JObject;
		if (dependencies == null)
		{
			dependencies = new JObject();
			root["dependencies"] = dependencies;
		}

		// Ignoring nulls and defaults keeps the written entry down to the fields that were actually
		// set - a PackageDependency has one field per source kind, and all but one are empty.
		var serializer = JsonSerializer.Create(new JsonSerializerSettings
		{
			NullValueHandling = NullValueHandling.Ignore,
			DefaultValueHandling = DefaultValueHandling.Ignore
		});
		dependencies[name] = JObject.FromObject(dependency, serializer);
		Log.Info($"[package] added {name} = {dependency.PinKey(name)}");
		return Save(projectRoot, root);
	}

	/// <summary>Removes one dependency. Returns true when the file changed.</summary>
	public static bool Remove(NPath projectRoot, string name)
	{
		var root = Load(projectRoot);
		if (root["dependencies"] is not JObject dependencies || dependencies.Remove(name) == false)
		{
			Log.Warning($"[package] {name} is not a dependency of this project; nothing to remove.");
			return false;
		}

		Log.Info($"[package] removed {name}");
		return Save(projectRoot, root);
	}

	private static JObject Load(NPath projectRoot)
	{
		var path = PackageManifest.PathIn(projectRoot);
		if (!path.FileExists())
		{
			return new JObject { ["name"] = projectRoot.FileName };
		}
		try
		{
			return JObject.Parse(path.ReadAllText());
		}
		catch (JsonException e)
		{
			throw new PackageException($"{path} is not valid JSON: {e.Message}", e);
		}
	}

	private static bool Save(NPath projectRoot, JObject root)
	{
		var path = PackageManifest.PathIn(projectRoot);
		// Null-valued fields come from the typed dependency's many optional sources; writing them
		// out would bury the two that matter in a wall of nulls.
		var content = JsonConvert.SerializeObject(root, Formatting.Indented,
			                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore })
		              + Environment.NewLine;
		if (path.FileExists() && path.ReadAllText() == content)
		{
			return false;
		}
		path.EnsureParentDirectoryExists();
		path.WriteAllText(content);
		return true;
	}
}
