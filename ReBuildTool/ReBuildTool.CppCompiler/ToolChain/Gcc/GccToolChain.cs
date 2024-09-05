using NiceIO;

namespace ReBuildTool.ToolChain;

public partial class GccToolChain : IToolChain
{
	public GccToolChain(BuildConfiguration configuration, Architecture arch) : base(configuration, arch)
	{
	}

	public override string Name => "Gcc";

	public override Dictionary<string, string> EnvVars()
	{
		throw new NotImplementedException();
	}

	public override IEnumerable<NPath> ToolChainIncludePaths()
	{
		throw new NotImplementedException();
	}

	public override IEnumerable<NPath> ToolChainLibraryPaths()
	{
		throw new NotImplementedException();
	}

	public override IEnumerable<string> ToolChainStaticLibraries()
	{
		throw new NotImplementedException();
	}

	public override IEnumerable<string> ToolChainDynamicLibraries()
	{
		throw new NotImplementedException();
	}
	
	public override ICompileArgsBuilder MakeCompileArgsBuilder()
	{
		return new GccCompileArgsBuilder();
	}

	public override ILinkArgsBuilder MakeLinkArgsBuilder()
	{
		return new GccLinkArgsBuilder();
	}

	public override IArchiveArgsBuilder MakeArchiveArgsBuilder()
	{
		return new GccArchiveArgsBuilder();
	}

	public override string ObjectExtension => ".o";
	public override string ExecutableExtension => string.Empty;
	public override string StaticLibraryExtension => ".a";
	public override string DynamicLibraryExtension => ".so";

}