using System.Text;

namespace ReBuildTool.Service.Global;

/// <summary>
/// Shell-level quoting for argv tokens that get re-joined into a single command-line
/// string and run through a shell (make/nmake recipes, ninja <c>command</c> lines,
/// diagnostic log lines). This is the counterpart to the per-token auto-quoting that
/// <c>ProcessStartInfo.ArgumentList</c> applies on the direct-build path: here a token must
/// survive the target shell's argv parse, so tokens containing spaces or metacharacters are
/// wrapped in double quotes with the specials inside escaped.
/// <para>
/// The rules are platform-specific because the shell is: on POSIX the tokens go to
/// <c>sh</c>, where <c>\</c> is an escape character; on Windows they go to <c>cmd</c>,
/// which does NOT understand backslash escapes, and the receiving program splits the
/// command line with the C runtime's rules (a backslash is literal unless it precedes a
/// double quote). Escaping every <c>\</c> the sh way on Windows would turn every path into
/// <c>"D:\\a\\b"</c> — which mostly survives only because Win32 collapses duplicate
/// separators, and breaks outright for UNC paths (<c>\\server\share</c>).
/// </para>
/// <para>
/// This layer knows nothing about make or ninja. A token that also has to survive one of
/// those file formats gets a second, format-specific escape applied on top of the result by
/// <c>MakeFileGenerator</c> / <c>NinjaFileGenerator</c> (e.g. <c>$</c> → <c>$$</c>).
/// </para>
/// </summary>
public static class ShellQuote
{
	/// <summary>
	/// Quotes a single argv token for a shell command line on the current platform.
	/// Returns the token unchanged when it contains no shell-significant characters.
	/// </summary>
	public static string ForArgument(string token)
	{
		return PlatformHelper.IsWindows() ? ForArgumentWindows(token) : ForArgumentPosix(token);
	}

	/// <summary>
	/// Quotes a program path for a shell command line. Equivalent to <see cref="ForArgument"/>
	/// since a program name is just the first argv token; kept as a separate name for
	/// readability at call sites.
	/// </summary>
	public static string ForProgram(string program) => ForArgument(program);

	/// <summary>
	/// <c>sh</c>-flavoured quoting: wrap in double quotes and backslash-escape the four
	/// characters that keep their meaning inside a double-quoted sh string
	/// (<c>\</c>, <c>"</c>, <c>`</c>, <c>$</c>). Exposed (rather than picked by platform)
	/// so it can be tested on any host.
	/// </summary>
	public static string ForArgumentPosix(string token)
	{
		if (string.IsNullOrEmpty(token))
		{
			// An empty token still has to occupy an argv slot.
			return "\"\"";
		}

		if (!NeedsQuotingPosix(token))
		{
			return token;
		}

		var sb = new StringBuilder(token.Length + 2);
		sb.Append('"');
		foreach (var c in token)
		{
			switch (c)
			{
				case '\\':
				case '"':
				case '`':
				case '$':
					sb.Append('\\');
					break;
			}
			sb.Append(c);
		}
		sb.Append('"');
		return sb.ToString();
	}

	/// <summary>
	/// Windows quoting: wrap in double quotes, and escape backslashes ONLY where the C
	/// runtime would otherwise consume them — i.e. a run of backslashes immediately before a
	/// literal <c>"</c> or before the closing quote gets doubled; every other backslash is
	/// passed through untouched. This is the same rule .NET applies when it builds a command
	/// line from <c>ProcessStartInfo.ArgumentList</c>, so both build paths quote identically.
	/// Exposed (rather than picked by platform) so it can be tested on any host.
	/// <para>
	/// Caveat: <c>cmd</c> expands <c>%VAR%</c> even inside double quotes, so a token
	/// containing <c>%</c> is not protected by quoting alone. Paths with <c>%</c> are not
	/// supported by the generated makefile/ninja backends.
	/// </para>
	/// </summary>
	public static string ForArgumentWindows(string token)
	{
		if (string.IsNullOrEmpty(token))
		{
			return "\"\"";
		}

		if (!NeedsQuotingWindows(token))
		{
			return token;
		}

		var sb = new StringBuilder(token.Length + 2);
		sb.Append('"');
		var i = 0;
		while (i < token.Length)
		{
			var backslashes = 0;
			while (i < token.Length && token[i] == '\\')
			{
				backslashes++;
				i++;
			}

			if (i == token.Length)
			{
				// Trailing run: doubled so the closing quote we are about to append is not
				// escaped away by it.
				sb.Append('\\', backslashes * 2);
				break;
			}

			if (token[i] == '"')
			{
				// 2n+1 backslashes then a quote => n literal backslashes + a literal quote.
				sb.Append('\\', backslashes * 2 + 1);
			}
			else
			{
				// Not before a quote: backslashes are literal, leave the run alone.
				sb.Append('\\', backslashes);
			}
			sb.Append(token[i]);
			i++;
		}
		sb.Append('"');
		return sb.ToString();
	}

	/// <summary>
	/// Strips a single wrapping pair of double quotes from <paramref name="value"/> (e.g.
	/// the <c>InQuotes()</c>-wrapped object paths that PrepareLinkUnit/PrepareArchiveUnit
	/// write into <c>.rsp</c> files), returning the clean inner token. A value without a
	/// surrounding quote pair is returned unchanged; this never touches quotes that are
	/// genuinely part of the data.
	/// </summary>
	public static string UnwrapQuotes(string value)
	{
		if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
		{
			return value.Substring(1, value.Length - 2);
		}
		return value;
	}

	private static bool NeedsQuotingPosix(string token)
	{
		foreach (var c in token)
		{
			// Whitespace forces quoting; the rest are shell metacharacters that would be
			// interpreted if left bare (or, for quotes themselves, would unbalance quoting).
			// Note: '=' is intentionally NOT included — it is literal in a command-word
			// position (only special as a shell assignment like FOO=bar), and flag tokens
			// such as /DNAME=value or --sysroot=path must stay unquoted.
			if (char.IsWhiteSpace(c) || c == '"' || c == '\'' || c == '\\' ||
			    c == '$' || c == '`' || c == '|' || c == '&' || c == ';' ||
			    c == '<' || c == '>' || c == '(' || c == ')' || c == '*' ||
			    c == '?' || c == '[' || c == ']' || c == '~' || c == '!' ||
			    c == '#')
			{
				return true;
			}
		}
		return false;
	}

	private static bool NeedsQuotingWindows(string token)
	{
		foreach (var c in token)
		{
			// Whitespace and the quote itself force quoting (argv splitting), plus the cmd
			// metacharacters that would otherwise be interpreted before the program ever sees
			// them. '\' is deliberately NOT here: it is an ordinary path separator on Windows
			// and quoting on account of it would quote literally every path.
			if (char.IsWhiteSpace(c) || c == '"' || c == '&' || c == '|' ||
			    c == '<' || c == '>' || c == '^' || c == '(' || c == ')')
			{
				return true;
			}
		}
		return false;
	}
}
