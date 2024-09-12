
using NiceIO;
using ReBuildTool.Service.CompileService;

namespace ReBuildTool.ToolChain;

public partial class CppBuilder
{
    internal partial class CompileProcess
    {

        public void GenerateMakeFile()
        {
            var makeFile = new MakeFileGenerator();
            CollectCompileTarget(makeFile);
            if (Module.TargetBuildType != BuildType.StaticLibrary)
            {
                CollectLinkTarget(makeFile);
            }
            else
            {
                CollectArchiveTarget(makeFile);
            }
            makeFile.FlushToFile(MakeFileCachePath());
        }

        private void CollectCompileTarget(MakeFileGenerator generator)
        {
            if (!CollectCompileUnit())
            {
                throw new Exception("can't collect compile unit");
            }

            if (!CollectCompileInvocations())
            {
                throw new Exception("can't collect compile invocations");
            }
            
            foreach (var invocation in CompileInvocation)
            {
                var unit = invocation.Unit;
                var depFiles = GetAllCompileUnitDep(unit);
                var target = new MakeFileGenerator.Target(unit.OutputFile.InQuotes(), MakeFileGenerator.TargetType.SubTarget);
                target.Dependencies.Add(unit.SourceFile.InQuotes());
                foreach (var depFile in depFiles)
                {
                    target.Dependencies.Add(depFile.InQuotes());
                }
                target.Invocations.Add(invocation.ToString());
                generator.Targets.Add(target);
            }
            
        }
        
        private void CollectLinkTarget(MakeFileGenerator generator)
        {
            if (!PrepareLinkUnit())
            {
                throw new Exception("PrepareLinkUnit failed.");
            }

            if (!PrepareLinkInvocation())
            {
                throw new Exception("PrepareLinkInvocation failed.");
            }

            var unit = LinkInvocation.Unit;
            var target = new MakeFileGenerator.Target(unit.OutputPath.InQuotes(), MakeFileGenerator.TargetType.MainTarget);
            foreach (var objFile in unit.ObjectFiles)
            {
                target.Dependencies.Add(objFile);
            }
            foreach (var libraryPath in unit.LibraryPaths)
            {
                foreach (var library in unit.DynamicLibraries)
                {
                    var libPath = libraryPath.Combine(library);
                    if (libPath.Exists())
                    {
                        target.Dependencies.Add(libPath.InQuotes());
                    }
                }
                foreach (var library in unit.StaticLibraries)
                {
                    var libPath = libraryPath.Combine(library);
                    if (libPath.Exists())
                    {
                        target.Dependencies.Add(libPath.InQuotes());
                    }
                }
            }
            target.Invocations.Add(LinkInvocation.ToString());
            generator.Targets.Add(target);
        }

        private void CollectArchiveTarget(MakeFileGenerator generator)
        {
            if (!PrepareArchiveUnit())
            {
                throw new Exception("PrepareArchiveUnit failed.");
            }
            
            if (!PrepareArchiveInvocation())
            {
                throw new Exception("PrepareArchiveInvocation failed.");
            }
            
            var unit = ArchiveInvocation.Unit;
            var target = new MakeFileGenerator.Target(unit.OutputPath.InQuotes(), MakeFileGenerator.TargetType.MainTarget);
            foreach (var objFile in unit.ObjectFiles)
            {
                target.Dependencies.Add(objFile);
            }
            foreach (var libraryPath in unit.LibraryPaths)
            {
                foreach (var library in unit.StaticLibraries)
                {
                    var libPath = libraryPath.Combine(library);
                    if (libPath.Exists())
                    {
                        target.Dependencies.Add(libPath.InQuotes());
                    }
                }
            }
            target.Invocations.Add(ArchiveInvocation.ToString());
            generator.Targets.Add(target);
        }
        
        
        private NPath MakeFileCachePath()
        {
            var result = Source.ProjectRoot
                .Combine("Intermedia", IPlatformSupport.CurrentTargetPlatform.ToString())
                .Combine(Options.Configuration.ToString())
                .Combine(Options.Architecture.Name)
                .Combine("MakeFileCache", Module.TargetName, "makefile");
            result.EnsureParentDirectoryExists();
            return result;
        }
        
        private List<NPath> GetAllCompileUnitDep(CppCompilationUnit unit)
        {
            var result = new List<NPath>();
            var includeInfos = unit.SourceFile.ReadAllLines()
                .Where(l => l.StartsWith("#include"))
                .Select(l =>
                {
                    var parts = l.Split(' ');
                    if (parts.Length == 2)
                    {
                        var fileName = parts[1];
                        return fileName.Substring(1, fileName.Length - 2);
                    }
                    return string.Empty;
                })
                .Where(n => !string.IsNullOrEmpty(n))
                .ToList();
            
            foreach (var includePath in unit.IncludePaths)
            {
                foreach (var includeInfo in includeInfos)
                {
                    var file = includePath.Combine(includeInfo);
                    if (file.Exists())
                    {
                        result.Add(file);
                    }
                }
            }

            return result;
        }
    }
}
    
    