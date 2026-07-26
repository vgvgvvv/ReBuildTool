namespace ReBuildTool.Service.Global;

/// <summary>
/// Shell-level quoting for argv tokens that get re-joined into a single command-line
/// string and run through a shell (make/nmake recipes, diagnostic log lines). This is the
/// counterpart to the per-token auto-quoting that <c>ProcessStartInfo.ArgumentList</c>
/// applies on the direct-build path and that <c>NinjaFileGenerator.NinjaVar</c> applies on
/// the ninja path: here a token must survive a real shell's argv parse, so tokens that
/// contain spaces or shell metacharacters are wrapped in double quotes with the shell
/// specials inside escaped.
/// <para>
/// The rule is intentionally sh-compatible (also the make default on POSIX, and works on
/// Windows where make typically drives cl/link via cmd): a token is left bare when it is
/// already shell-safe, otherwise it becomes <c>"..."</c> with every <c>\</c>, <c>"</c>,
/// <c>`</c> and <c>$</c> escaped. Paths produced by the toolchains (the common case) need
/// quoting only when they contain spaces.
/// </para>
/// </summary>
public static class ShellQuote
{
	/// <summary>
	/// Quotes a single argv token for a shell command line. Returns the token unchanged
	/// when it contains no shell-significant characters.
	/// </summary>
	public static string ForArgument(string token)
	{
		if (string.IsNullOrEmpty(token))
		{
			return token;
		}

		// Bare token is fine unless it has whitespace or a character a shell would interpret.
		if (!NeedsQuoting(token))
		{
			return token;
		}

		// Escape the characters that are special inside a double-quoted sh string, then wrap.
		var sb = new System.Text.StringBuilder(token.Length + 2);
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
	/// Quotes a program path for a shell command line. Equivalent to <see cref="ForArgument"/>
	/// since a program name is just the first argv token; kept as a separate name for
	/// readability at call sites.
	/// </summary>
	public static string ForProgram(string program) => ForArgument(program);

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

	private static bool NeedsQuoting(string token)
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
}
