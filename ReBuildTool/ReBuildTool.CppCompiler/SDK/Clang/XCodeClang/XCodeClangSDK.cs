using NiceIO;

namespace ReBuildTool.ToolChain.SDK.XCodeClang;

public class XCodeClangSDK : ClangSDK
{

	public XCodeClangSDK() : this(new NPath(""))
	{
	}
	public XCodeClangSDK(NPath xcodeLocation) : base(xcodeLocation)
	{
		XCodeLocation = xcodeLocation;
		if (!XCodeLocation.Exists())
		{
			XCodeLocation = "/Applications/Xcode.app".ToNPath();
		}
	}
	

	public NPath XCodeLocation { get; }
	
	public NPath XCodeClangLocation => XCodeLocation.Combine("Content/Developer/Toolchains/XcodeDefault.xctoolchain/usr/bin/clang");
	
	public NPath DefaultClangLocation { get; } = "usr/bin/clang".ToNPath();
	
	public override IEnumerable<ICppLibrary> GetCppLibs(Architecture arch)
	{
		yield return new XCodeCppLibrary(this);
	}

	public override NPath GetCompiler()
	{
		return FindClang();
	}

	public override NPath GetLinker()
	{
		return FindClang();
	}

	public override NPath GetArchiver()
	{
		return FindClang();
	}

	private NPath FindClang()
	{
		if (XCodeLocation.DirectoryExists())
		{
			return XCodeClangLocation;
		}
		else if (DefaultClangLocation.Exists())
		{
			return DefaultClangLocation;
		}
		else
		{
			throw new Exception("Clang not found");
		}
	}

	internal class XCodeCppLibrary : ICppLibrary
	{
		public XCodeCppLibrary(XCodeClangSDK owner)
		{
			Owner = owner;
		}
		
		public IEnumerable<NPath> IncludePaths()
		{
			if (!Owner.XCodeLocation.Exists())
			{
				throw new Exception("XCode location not found");
			}
			yield return Owner.XCodeLocation.Combine(
				"Contents/Developer/Platforms/MacOSX.platform/Developer/SDKs/MacOSX.sdk/usr/include");
		}

		public IEnumerable<NPath> LibraryPaths()
		{
			yield break;	
		}

		public IEnumerable<string> StaticLibraries()
		{
			yield break;
		}

		public IEnumerable<string> DynamicLibraries()
		{
			yield break;
		}

		private XCodeClangSDK Owner;
	}
}