using NiceIO;
using ReBuildTool.ToolChain.SDK;

namespace ReBuildTool.ToolChain;

public partial class GccToolChain : IToolChain
{
	public GccToolChain(BuildConfiguration configuration, Architecture arch) : base(configuration, arch)
	{
	}

	public override string Name => "Gcc";

	public LinuxSDK LinuxSdk { get; } = new();

	public override Dictionary<string, string> EnvVars()
	{
		return new()
		{
			
		};
	}

	public override IEnumerable<NPath> ToolChainIncludePaths()
	{
		foreach (var includePath in LinuxSdk.IncludePaths())
		{
			yield return includePath;
		}
	}

	public override IEnumerable<NPath> ToolChainLibraryPaths()
	{
		foreach (var libraryPath in LinuxSdk.LibraryPaths())
		{
			yield return libraryPath;
		}
	}

	public override IEnumerable<string> ToolChainStaticLibraries()
	{
		foreach (var library in LinuxSdk.StaticLibraries())
		{
			yield return library;
		}
	}

	public override IEnumerable<string> ToolChainDynamicLibraries()
	{
		foreach (var library in LinuxSdk.DynamicLibraries())
		{
			yield return library;
		}
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
	public override string LibraryPrefix => "lib";
}