using NiceIO;

namespace ReBuildTool.ToolChain.SDK;

// internal class Msvc15 : MsvcSDK
// {
// 	public static MsvcSDK Create(Version version, NPath path)
// 	{
// 		return new Msvc15(version, path);
// 	}
// 	
// 	protected Msvc15(Version version, NPath path) : base(version, path)
// 	{
// 	}
// 	
// 	public override IEnumerable<NPath> GetSdkIncludeDirectories(Architecture arch)
// 	{
// 		throw new NotImplementedException();
// 	}
//
// 	public override IEnumerable<NPath> GetSdkLibraryDirectories(Architecture arch)
// 	{
// 		throw new NotImplementedException();
// 	}
//
// 	public override IEnumerable<NPath> GetVcIncludeDirectories(Architecture arch)
// 	{
// 		throw new NotImplementedException();
// 	}
//
// 	public override IEnumerable<NPath> GetVcLibraryDirectories(Architecture arch)
// 	{
// 		throw new NotImplementedException();
// 	}
// }