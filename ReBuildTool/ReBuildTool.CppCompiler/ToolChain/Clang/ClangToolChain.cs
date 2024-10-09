using NiceIO;
using ReBuildTool.ToolChain.SDK;

namespace ReBuildTool.ToolChain;

public abstract partial class ClangToolChain : IToolChain
{
	public ClangToolChain(BuildConfiguration configuration, Architecture arch) : base(configuration, arch)
	{
	}

	public override string Name => "Clang";

	protected abstract ClangSDK ClangSdk { get; }

	public virtual IEnumerable<ICppLibrary> CppLibraries()
	{
		foreach (var cppLibrary in ClangSdk.GetCppLibs(Arch))
		{
			yield return cppLibrary;
		}
	}

	public override Dictionary<string, string> EnvVars()
	{
		return new()
		{
			{"PATH", Environment.GetEnvironmentVariable("PATH") ?? "" },
		};
	}

	public override IEnumerable<NPath> ToolChainIncludePaths()
	{
		foreach (var cppLibrary in CppLibraries())
		{
			foreach (var includePath in cppLibrary.IncludePaths())
			{
				yield return includePath;
			}
		}
	}

	public override IEnumerable<NPath> ToolChainLibraryPaths()
	{
		foreach (var cppLibrary in CppLibraries())
		{
			foreach (var libraryPath in cppLibrary.LibraryPaths())
			{
				yield return libraryPath;
			}
		}
	}

	public override IEnumerable<string> ToolChainStaticLibraries()
	{
		foreach (var cppLibrary in CppLibraries())
		{
			foreach (var includePath in cppLibrary.StaticLibraries())
			{
				yield return includePath;
			}
		}
	}

	public override IEnumerable<string> ToolChainDynamicLibraries()
	{
		foreach (var cppLibrary in CppLibraries())
		{
			foreach (var includePath in cppLibrary.DynamicLibraries())
			{
				yield return includePath;
			}
		}
	}
	
	public override ICompileArgsBuilder MakeCompileArgsBuilder()
	{
		return new GccLikeClangCompileArgsBuilder();
	}
	
	public override ILinkArgsBuilder MakeLinkArgsBuilder()
	{
		return new GccLikeClangLinkArgsBuilder();
	}
	
	public override IArchiveArgsBuilder MakeArchiveArgsBuilder()
	{
		return new GccLikeClangArchiveArgsBuilder();
	}
}