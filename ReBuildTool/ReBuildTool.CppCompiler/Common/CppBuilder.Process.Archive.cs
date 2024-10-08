using System.Text;
using NiceIO;
using ReBuildTool.Service.CompileService;
using ResetCore.Common;

namespace ReBuildTool.ToolChain;

public partial class CppBuilder
{
	public partial class CompileProcess
	{
		public bool Archive()
		{
			if (!PrepareArchiveUnit())
			{
				Log.Error("Prepare ArchiveUnit failed.");
				return false;
			}

			if (!PrepareArchiveInvocation())
			{
				Log.Error("Prepare ArchiveInvocation failed.");
				return false;
			}

			if (!RunArchiveInvocation())
			{
				Log.Error("Run ArchiveInvocation failed.");
				return false;
			}

			return true;
		}

		private NPath ArchiveResultPath()
		{
			return Source.OutputRoot
				.Combine(IPlatformSupport.CurrentTargetPlatform.ToString())
				.Combine(Options.Configuration.ToString())
				.Combine(Options.Architecture.Name)
				.Combine(ToolChain.LibraryPrefix + Module.TargetName + ToolChain.StaticLibraryExtension);
		}
		
		private bool PrepareArchiveUnit()
		{
			ArchiveUnit = new CppArchiveUnit();
			ArchiveUnit.ObjectFiles = CompileUnits.Select(cu => cu.OutputFile).ToList();
			ArchiveUnit.ArchiveFlags = GetArchiveFlagsForArchiveUnit(ArchiveUnit);
			ArchiveUnit.StaticLibraries = GetStaticLibraryForArchiveUnit(ArchiveUnit);
			ArchiveUnit.LibraryPaths = GetLibrarySearchPathForArchiveUnit(ArchiveUnit);
			ArchiveUnit.OutputPath = ArchiveResultPath();
			ArchiveUnit.ResponseFile = ArchiveUnit.OutputPath.ChangeExtension(".rsp");
			var rspContent = string.Join(Environment.NewLine, ArchiveUnit.ObjectFiles.InQuotes());
			ArchiveUnit.ResponseFile.EnsureParentDirectoryExists();
			File.WriteAllText(ArchiveUnit.ResponseFile, rspContent, Encoding.UTF8);
			ArchiveUnit.ArchiveArgsBuilder = ToolChain.MakeArchiveArgsBuilder();
			if(Module is CppModuleRule moduleRule)
			{
				moduleRule.AdditionArchiveArgs(ArchiveUnit.ArchiveArgsBuilder);
			}
			return true;
		}

		private bool PrepareArchiveInvocation()
		{
			ArchiveInvocation = ToolChain.MakeArchiveInvocation(ArchiveUnit);
			return true;
		}

		private bool RunArchiveInvocation()
		{
			if (!ArchiveInvocation.Run())
			{
				Log.Error($"{ArchiveInvocation.ProgramName} {ArchiveInvocation.Arguments.Join(" ")}");
				Log.Error($"Archive {Module.TargetName} failed !");
				return false;
			}

			return true;
		}

		private IEnumerable<string> GetStaticLibraryForArchiveUnit(CppArchiveUnit unit)
		{
			foreach (var staticLibrary in GetStaticLibrariesForModule(Module))
			{
				yield return staticLibrary;
			}

			foreach (var staticLibrary in Options.CustomStaticLibraries)
			{
				yield return staticLibrary;
			}
		}

		private IEnumerable<NPath> GetLibrarySearchPathForArchiveUnit(CppArchiveUnit unit)
		{
			foreach (var path in GetLibraryDirectoriesForModule(Module))
			{
				yield return path.ToNPath();
			}

			foreach (var libraryDirectory in Options.CustomLibraryDirectories)
			{
				yield return libraryDirectory;
			}
		}

		private IEnumerable<string> GetArchiveFlagsForArchiveUnit(CppArchiveUnit unit)
		{
			foreach (var flag in GetArchiveFlagsForModule(Module))
			{
				yield return flag;
			}

			foreach (var flag in Options.CustomArchiveFlags)
			{
				yield return flag;
			}
		}
		
		private CppArchiveUnit ArchiveUnit { get; set; }
		
		private CppArchiveInvocation ArchiveInvocation { get; set; }
		
	}
}