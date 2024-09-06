using NiceIO;

namespace ReBuildTool.ToolChain.SDK;

public class LinuxSDK : ICppLibrary
{
    public string CppLibVersion { get; } = "7";
    
    public IEnumerable<NPath> IncludePaths()
    {
        yield return new NPath("/usr/include");
        yield return new NPath("/usr/local/include");
        var cppIncludeRoot = new NPath("/usr/include/c++");
        var targetVersion = cppIncludeRoot.Combine(CppLibVersion);
        if (targetVersion.Exists())
        {
            yield return targetVersion;
        }
        else
        {
            var cppInclude = cppIncludeRoot.Directories().FirstOrDefault();
            if (cppInclude != null && cppInclude.Exists())
            {
                yield return cppInclude;
            }
        }
    }

    public IEnumerable<NPath> LibraryPaths()
    {
        yield return new NPath("/usr/lib");
        yield return new NPath("/usr/local/lib");
    }

    public IEnumerable<string> StaticLibraries()
    {
        yield break;
    }

    public IEnumerable<string> DynamicLibraries()
    {
        yield break;
    }

    public NPath GetCompiler(NPath sourceFile)
    {
        var ex = sourceFile.ExtensionWithDot;
        if(ex == ".c" || ex == ".cpp" || ex == ".cc" || ex == ".cxx")
        {
            return new NPath("/usr/bin/g++");
        }
        else if (ex == ".asm")
        {
            return new NPath("/usr/bin/as");
        }
        else
        {
            throw new NotImplementedException($"Unsupported file type {ex}");
        }
    }

    public NPath GetLinker()
    {
        return new NPath("/usr/bin/ld");
    }

    public NPath GetArchiver()
    {
        return new NPath("/usr/bin/ar");
    }
}