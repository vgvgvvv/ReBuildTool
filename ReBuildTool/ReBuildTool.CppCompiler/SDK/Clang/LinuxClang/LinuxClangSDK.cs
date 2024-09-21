using NiceIO;

namespace ReBuildTool.ToolChain.SDK;

public class LinuxClangSDK : ClangSDK
{
    public LinuxClangSDK(NPath root) : base(root)
    {
        LinuxSDK = new LinuxSDK();
    }
    
    private LinuxSDK LinuxSDK { get; set; }
    
    public override IEnumerable<ICppLibrary> GetCppLibs(Architecture arch)
    {
        yield return LinuxSDK;
    }

    public override NPath GetCompiler()
    {
        return RootPath.Combine("clang++");
    }

    public override NPath GetLinker()
    {
        return RootPath.Combine("clang++");
    }

    public override NPath GetArchiver()
    {
        return RootPath.Combine("clang++");
    }
}