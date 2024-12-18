﻿using NiceIO;
using ReBuildTool.ToolChain.SDK;
using ResetCore.Common;

namespace ReBuildTool.ToolChain;

public partial class MSVCToolChain : IToolChain
{
	public override string Name => "MSVC";
	private MsvcSDK msvcSdk { get; }
	
	public MSVCToolChain(BuildConfiguration configuration, Architecture arch) : base(configuration, arch)
	{
		msvcSdk = MsvcSDK.FindLatestSDK()
			.SetArch(arch)
			.UseLastestVCPaths()
			.UseLatestWindowsKit();
	}

	public override Dictionary<string, string> EnvVars()
	{
		return new Dictionary<string, string>()
		{
			{"PATH", string.Join(';', msvcSdk.PathEnvironmentVariable) + ";" + Environment.GetEnvironmentVariable("PATH")},
			{ "VSLANG", "1033" } // vcpkg use language english
		};
	}

	public override IEnumerable<NPath> ToolChainIncludePaths()
	{
		return msvcSdk.GetIncludeDirectories();
	}

	public override IEnumerable<NPath> ToolChainLibraryPaths()
	{
		return msvcSdk.GetLibraryDirectories();
	}

	public override IEnumerable<string> ToolChainStaticLibraries()
	{
		yield break; 
	}
	
	public override IEnumerable<string> ToolChainDynamicLibraries()
	{
		yield break; 
	}

	public override string ObjectExtension => ".obj";
	public override string ExecutableExtension => ".exe";
	public override string StaticLibraryExtension => ".lib";
	public override string DynamicLibraryExtension => ".dll";

	public override ICompileArgsBuilder MakeCompileArgsBuilder()
	{
		return new MSVCCompileArgsBuilder();
	}

	public override ILinkArgsBuilder MakeLinkArgsBuilder()
	{
		return new MSVCLinkArgsBuilder();
	}

	public override IArchiveArgsBuilder MakeArchiveArgsBuilder()
	{
		return new MSVCArchiveArgsBuilder();
	}
	
}