using NiceIO;

namespace ReBuildTool.ToolChain.SDK.XCodeClang;

public class XCodeClangSDK : ClangSDK
{
	public XCodeClangSDK(NPath root) : base(root)
	{
	}

	public override IEnumerable<ICppLibrary> GetCppLibs(Architecture arch)
	{
		yield break;
	}

	public override NPath GetCompiler()
	{
		throw new NotImplementedException();
	}

	public override NPath GetLinker()
	{
		throw new NotImplementedException();
	}

	public override NPath GetArchiver()
	{
		throw new NotImplementedException();
	}
}