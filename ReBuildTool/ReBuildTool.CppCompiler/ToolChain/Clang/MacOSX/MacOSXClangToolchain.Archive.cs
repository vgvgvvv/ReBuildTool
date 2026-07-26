using NiceIO;
using ReBuildTool.Service.Global;

namespace ReBuildTool.ToolChain;

public partial class MacOSXClangToolchain
{
    protected override IEnumerable<string> ArchiveArgsFor(CppArchiveUnit unit)
    {
        yield return "rcs";

        var linkBuilder = unit.ArchiveArgsBuilder;

        foreach (var argument in linkBuilder.GetAllArguments())
        {
            yield return argument;
        }

        foreach (var archiveFlag in unit.ArchiveFlags)
        {
            yield return archiveFlag;
        }

        yield return unit.OutputPath.ToString();

        // PrepareArchiveUnit writes one InQuotes()-wrapped object path per line into the
        // .rsp. We inline those object paths as command-line tokens (this toolchain doesn't
        // use @rsp), so unquote each line back to a clean argv token — downstream (Shell
        // ArgumentList, or ShellQuote for the ninja/makefile command lines) re-applies
        // quoting in the form its own target needs.
        var lines = File.ReadLines(unit.ResponseFile);
        foreach (var line in lines)
        {
            yield return ShellQuote.UnwrapQuotes(line);
        }

    }

}