using NiceIO;

namespace ReBuildTool.ToolChain.Wasm;

public partial class WasmToolchain : IToolChain
{

	public WasmToolchain(BuildConfiguration configuration, Architecture arch) : base(configuration, arch)
	{
	}

	public override string Name => "Wasm";

	

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



	public override string ObjectExtension => ".bc";

	public override string ExecutableExtension => ".html";

	public override string StaticLibraryExtension => ".a";

	public override string DynamicLibraryExtension => ".so";


	public override ICompileArgsBuilder MakeCompileArgsBuilder()
	{
		return new WasmCompileArgsBuilder();
	}

	public override ILinkArgsBuilder MakeLinkArgsBuilder()
	{
		return new WasmLinkArgsBuilder();
	}

	public override IArchiveArgsBuilder MakeArchiveArgsBuilder()
	{
		return new WasmArchiveArgsBuilder();
	}

}
