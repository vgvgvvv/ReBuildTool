﻿using NiceIO;

namespace ReBuildTool.ToolChain;

public class MSVCToolChain : IToolChain
{
	public MSVCToolChain(BuildConfiguration configuration, Architecture arch) : base(configuration, arch)
	{
	}

	public override IEnumerable<string> OutputArguments(NPath objectFile, NPath sourceFile)
	{
		throw new NotImplementedException();
	}

	public override IEnumerable<NPath> ToolChainDefines()
	{
		throw new NotImplementedException();
	}

	public override IEnumerable<NPath> EnvVars()
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

	public override IEnumerable<NPath> ToolChainStaticLibrarys()
	{
		throw new NotImplementedException();
	}

	public override string ObjectExtension { get; }
	public override string ExecutableExtension { get; }
	public override string StaticLibraryExtension { get; }
	public override string DynamicLibraryExtension { get; }
	public override NPath CompilerExecutableFor(NPath sourceFile)
	{
		throw new NotImplementedException();
	}

	public override IEnumerable<string> CompilerFlagsFor(CppCompilationUnit cppCompilationInstruction)
	{
		throw new NotImplementedException();
	}
}