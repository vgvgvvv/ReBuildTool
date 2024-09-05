using NiceIO;

namespace ReBuildTool.ToolChain.SDK;

public abstract class ClangSDK
{
	public NPath RootPath { get; }

	public ClangSDK(NPath root)
	{
		RootPath = root;
	}

	public abstract IEnumerable<ICppLibrary> GetCppLibs(Architecture arch);

	public abstract NPath GetCompiler();

	public abstract NPath GetLinker();
	
	public abstract NPath GetArchiver();
}