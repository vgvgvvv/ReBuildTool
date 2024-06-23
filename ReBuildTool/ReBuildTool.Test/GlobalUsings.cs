global using NUnit.Framework;
using NiceIO;

internal static class TestCaseGlobalVars
{
    private static NPath _rootDirectory = null;
    public static NPath RootDirectory
    {
        get
        {
            if (_rootDirectory == null)
            {
                var currentPath = Environment.CurrentDirectory.ToNPath();
                while (!currentPath.Combine("LICENSE").Exists())
                {
                    currentPath = currentPath.Parent;
                }

                _rootDirectory = currentPath;
            }

            return _rootDirectory;
        }
    }

    public static NPath SampleDirectory => RootDirectory.Combine("Sample");
}