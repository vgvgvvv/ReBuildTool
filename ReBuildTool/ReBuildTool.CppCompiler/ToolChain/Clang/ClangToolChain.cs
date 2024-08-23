using NiceIO;
using ReBuildTool.ToolChain.SDK;

namespace ReBuildTool.ToolChain;

public abstract partial class ClangToolChain : IToolChain
{
	public ClangToolChain(BuildConfiguration configuration, Architecture arch) : base(configuration, arch)
	{
	}

	public override string Name => "Clang";

	protected abstract ClangSDK clangSdk { get; }

	public override Dictionary<string, string> EnvVars()
	{
		return null;
	}

	public override IEnumerable<NPath> ToolChainIncludePaths()
	{
		return null;
	}

	public override IEnumerable<NPath> ToolChainLibraryPaths()
	{
		return null;
	}

	public override IEnumerable<NPath> ToolChainStaticLibraries()
	{
		return null;
	}

	public override IEnumerable<NPath> ToolChainDynamicLibraries()
	{
		return null;
	}
	
	public override ICompileArgsBuilder MakeCompileArgsBuilder()
	{
		return new ClangCompileArgsBuilder();
	}

	public override ILinkArgsBuilder MakeLinkArgsBuilder()
	{
		return new ClangLinkArgsBuilder();
	}

	public override IArchiveArgsBuilder MakeArchiveArgsBuilder()
	{
		return new ClangArchiveArgsBuilder();
	}
}