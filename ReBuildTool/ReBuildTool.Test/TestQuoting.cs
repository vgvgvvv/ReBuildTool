using ReBuildTool.Service.Global;
using ReBuildTool.ToolChain;

namespace ReBuildTool.Test;

/// <summary>
/// Unit tests for the quoting/escaping layers that carry a path from a toolchain's clean argv
/// token into a shell command line, a build.ninja and a Makefile. They run against synthetic
/// tokens rather than a generated project, so the interesting inputs (spaces, drive letters,
/// UNC, '$', embedded quotes) are covered on any host — the sample projects all live under
/// space-free paths and would never exercise them.
/// </summary>
public class QuotingTests
{
    // --- ShellQuote, POSIX flavour (sh: backslash IS an escape inside double quotes) ---

    [Test]
    public void PosixQuote_LeavesOrdinaryTokenBare()
    {
        Assert.That(ShellQuote.ForArgumentPosix("-I/usr/include"), Is.EqualTo("-I/usr/include"));
        Assert.That(ShellQuote.ForArgumentPosix("--sysroot=/opt/sysroot"), Is.EqualTo("--sysroot=/opt/sysroot"));
        Assert.That(ShellQuote.ForArgumentPosix("/DFOO=bar"), Is.EqualTo("/DFOO=bar"));
    }

    [Test]
    public void PosixQuote_WrapsTokenWithSpace()
    {
        Assert.That(ShellQuote.ForArgumentPosix("-I/home/me/My Dir/inc"),
            Is.EqualTo("\"-I/home/me/My Dir/inc\""));
    }

    [Test]
    public void PosixQuote_EscapesSpecialsInsideDoubleQuotes()
    {
        // Inside "..." sh still acts on \ " ` $ — each has to be backslash-escaped.
        Assert.That(ShellQuote.ForArgumentPosix("a b$c"), Is.EqualTo("\"a b\\$c\""));
        Assert.That(ShellQuote.ForArgumentPosix("a b`c"), Is.EqualTo("\"a b\\`c\""));
        Assert.That(ShellQuote.ForArgumentPosix("-DNAME=\"a b\""), Is.EqualTo("\"-DNAME=\\\"a b\\\"\""));
    }

    [Test]
    public void PosixQuote_EmptyTokenStillOccupiesAnArgvSlot()
    {
        Assert.That(ShellQuote.ForArgumentPosix(""), Is.EqualTo("\"\""));
    }

    // --- ShellQuote, Windows flavour (cmd has no backslash escape; the CRT splits argv) ---

    [Test]
    public void WindowsQuote_LeavesPlainPathBare()
    {
        // The regression this guards: quoting on account of '\' quoted (and backslash-doubled)
        // literally every Windows path, turning D:\a\b into "D:\\a\\b".
        Assert.That(ShellQuote.ForArgumentWindows(@"/ID:\proj\include"), Is.EqualTo(@"/ID:\proj\include"));
        Assert.That(ShellQuote.ForArgumentWindows(@"D:\proj\src\main.cpp"), Is.EqualTo(@"D:\proj\src\main.cpp"));
    }

    [Test]
    public void WindowsQuote_WrapsPathWithSpaceWithoutTouchingSeparators()
    {
        Assert.That(ShellQuote.ForArgumentWindows(@"D:\Space Test\main.cpp"),
            Is.EqualTo("\"D:\\Space Test\\main.cpp\""));
    }

    [Test]
    public void WindowsQuote_KeepsUncPathIntact()
    {
        // Doubling every backslash turned \\server\share into \\\\server\share, which is not a
        // valid UNC prefix — the leading pair must survive verbatim.
        Assert.That(ShellQuote.ForArgumentWindows(@"\\server\share\a.cpp"),
            Is.EqualTo(@"\\server\share\a.cpp"));
        Assert.That(ShellQuote.ForArgumentWindows(@"\\server\my share\a.cpp"),
            Is.EqualTo("\"\\\\server\\my share\\a.cpp\""));
    }

    [Test]
    public void WindowsQuote_DoublesBackslashesOnlyWhereTheCrtWouldEatThem()
    {
        // Trailing run: doubled so it escapes itself instead of the closing quote.
        Assert.That(ShellQuote.ForArgumentWindows(@"/ID:\Space Test\inc\"),
            Is.EqualTo("\"/ID:\\Space Test\\inc\\\\\""));
        // Run before a literal quote: 2n+1 backslashes so the quote stays data.
        Assert.That(ShellQuote.ForArgumentWindows("-DNAME=\"a b\""),
            Is.EqualTo("\"-DNAME=\\\"a b\\\"\""));
    }

    [Test]
    public void WindowsQuote_QuotesCmdMetacharacters()
    {
        Assert.That(ShellQuote.ForArgumentWindows("a&b"), Is.EqualTo("\"a&b\""));
        Assert.That(ShellQuote.ForArgumentWindows("a|b"), Is.EqualTo("\"a|b\""));
        Assert.That(ShellQuote.ForArgumentWindows("a^b"), Is.EqualTo("\"a^b\""));
        Assert.That(ShellQuote.ForArgumentWindows("a>b"), Is.EqualTo("\"a>b\""));
    }

    [Test]
    public void UnwrapQuotes_StripsOnlyAWrappingPair()
    {
        Assert.That(ShellQuote.UnwrapQuotes("\"D:\\a b\\x.obj\""), Is.EqualTo(@"D:\a b\x.obj"));
        Assert.That(ShellQuote.UnwrapQuotes(@"D:\a\x.obj"), Is.EqualTo(@"D:\a\x.obj"));
        // Not a wrapping pair: leave the data alone.
        Assert.That(ShellQuote.UnwrapQuotes("-DNAME=\"a b\""), Is.EqualTo("-DNAME=\"a b\""));
    }

    // --- Ninja escaping ---

    [Test]
    public void NinjaPath_EscapesDriveLetterAndEachSpaceIndividually()
    {
        // Per-space "$ ", NOT a "$ ... $" wrapper around the whole path (which made ninja
        // report "expected build command name").
        Assert.That(NinjaFileGenerator.NinjaPathToken(@"C:\My Dir\x.o"), Is.EqualTo("C$:/My$ Dir/x.o"));
        Assert.That(NinjaFileGenerator.NinjaPathToken("/home/me/My Dir/x.o"),
            Is.EqualTo("/home/me/My$ Dir/x.o"));
    }

    [Test]
    public void NinjaPath_DoublesLiteralDollarBeforeAddingItsOwnEscapes()
    {
        // '$' is ninja's escape character: a literal one must become '$$', and the '$' that
        // the ':' and ' ' escapes introduce must not be doubled in turn.
        Assert.That(NinjaFileGenerator.NinjaPathToken("/home/me/a$b/x.o"), Is.EqualTo("/home/me/a$$b/x.o"));
        Assert.That(NinjaFileGenerator.NinjaPathToken(@"C:\a$b c\x.o"), Is.EqualTo("C$:/a$$b$ c/x.o"));
    }

    [Test]
    public void NinjaValue_DoublesDollarAndLeavesShellQuotingAlone()
    {
        // Variable values are shell fragments: colons and spaces are literal there, only '$'
        // is ninja-significant.
        Assert.That(NinjaFileGenerator.NinjaValue("\"D:\\Space Test\\cl.exe\" /c"),
            Is.EqualTo("\"D:\\Space Test\\cl.exe\" /c"));
        Assert.That(NinjaFileGenerator.NinjaValue("cc \"-I/a$b\""), Is.EqualTo("cc \"-I/a$$b\""));
    }
}
