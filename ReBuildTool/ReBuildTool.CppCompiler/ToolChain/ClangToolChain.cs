using NiceIO;

namespace ReBuildTool.ToolChain;

public class ClangToolChain : IToolChain
{
	public ClangToolChain(BuildConfiguration configuration, Architecture arch) : base(configuration, arch)
	{
	}

	public override IEnumerable<string> ObjectOutputArguments(NPath objectFile, NPath sourceFile)
	{
		yield return "-c";
		yield return sourceFile;
		yield return "-o";
		yield return objectFile;
	}

	public override IEnumerable<string> ToolChainDefines()
	{
		yield break;
	}

	public override IEnumerable<NPath> EnvVars()
	{
		yield break;
	}

	public override IEnumerable<NPath> ToolChainIncludePaths()
	{
		yield break;
	}

	public override IEnumerable<NPath> ToolChainLibraryPaths()
	{
		yield break;
	}

	public override IEnumerable<NPath> ToolChainStaticLibraries()
	{
		yield break;
	}

	public override bool CanBeCompiled(NPath sourceFile)
	{
		throw new NotImplementedException();
	}

	public override string ObjectExtension => ".o";
	public override string ExecutableExtension => string.Empty;
	public override string StaticLibraryExtension => ".a";
	public override string DynamicLibraryExtension => ".so";
	public override NPath CompilerExecutableFor(NPath sourceFile)
	{
		if (sourceFile.ExtensionWithDot == ".cpp" || 
		    sourceFile.ExtensionWithDot == ".cc")
		{
			return "clang++".ToNPath();
		}
		else if(sourceFile.ExtensionWithDot == ".c")
		{
			return "clang".ToNPath();
		}
		throw new NotSupportedException($"Unsupported file type {sourceFile.ExtensionWithDot}");
	}

	public override IEnumerable<string> CompilerFlagsFor(CppCompilationUnit cppCompilationInstruction)
	{
		yield break;
	}
}