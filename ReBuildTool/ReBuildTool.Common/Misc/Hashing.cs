using System.Security.Cryptography;
using NiceIO;

namespace ReBuildTool.Service.Global;

/// <summary>
/// Content hashing for downloaded artifacts.
///
/// rbt's incremental machinery is timestamp based everywhere else, which is right for build
/// outputs derived from local sources. It is not enough for bytes pulled off the network: a
/// download has to be checked against what the manifest said it should be, before it is trusted
/// enough to unpack.
/// </summary>
public static class Hashing
{
	public static string Sha256Of(NPath file)
	{
		using var stream = File.OpenRead(file.ToString());
		return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
	}

	/// <summary>
	/// Compares a computed hash against an expected one, tolerating the common spellings users
	/// paste in: mixed case, and an explicit "sha256:" prefix.
	/// </summary>
	public static bool Matches(string expected, string actual)
	{
		var normalized = expected.Trim();
		if (normalized.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
		{
			normalized = normalized.Substring("sha256:".Length);
		}
		return string.Equals(normalized, actual, StringComparison.OrdinalIgnoreCase);
	}
}
