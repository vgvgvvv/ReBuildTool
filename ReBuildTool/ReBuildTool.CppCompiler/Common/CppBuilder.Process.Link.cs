using System.Text;
using NiceIO;
using ReBuildTool.Service.CompileService;
using ResetCore.Common;

namespace ReBuildTool.ToolChain;

public partial class CppBuilder
{
	public partial class CompileProcess
	{
		public bool Link()
		{
			if (!PrepareLinkUnit())
			{
				Log.Error("PrepareLinkUnit failed.");
				return false;
			}

			if (!PrepareLinkInvocation())
			{
				Log.Error("PrepareLinkInvocation failed.");
				return false;
			}

			if (!RunLinkInvocations())
			{
				Log.Error("RunLinkInvocations failed.");
				return false;
			}
			
			return true;
		}

		private bool PrepareLinkUnit()
		{
			LinkUnit = new CppLinkUnit();
			LinkUnit.ObjectFiles = CompileUnits.Select(cu => cu.OutputFile).ToList();
			LinkUnit.LinkFlags = GetLinkFlagsForLinkUnit(LinkUnit);
			LinkUnit.DynamicLibraries = GetDynamicLibrariesForLinkUnit(LinkUnit);
			LinkUnit.StaticLibraries = GetStaticLibrariesForLinkUnit(LinkUnit);
			LinkUnit.LibraryPaths = GetLibrarySearchPathForLinkUnit(LinkUnit);
			LinkUnit.OutputPath = LinkResultPath();
			LinkUnit.ResponseFile = LinkUnit.OutputPath.ChangeExtension(".rsp");
			var normalized = LinkUnit.ObjectFiles.Select(objFilePath => objFilePath.InQuotes().ToString().Replace("\\", "/"));
			var rspContent = string.Join(Environment.NewLine, normalized);
			LinkUnit.ResponseFile.EnsureParentDirectoryExists();
			File.WriteAllText(LinkUnit.ResponseFile, rspContent, new UTF8Encoding(false));
			if (Module is CppModuleRule moduleRule)
			{
				LinkUnit.LinkArgsBuilder = ToolChain.MakeLinkArgsBuilder();
				moduleRule.AdditionLinkArgs(LinkUnit.LinkArgsBuilder);
			}
			return true;
		}
		
		private bool PrepareLinkInvocation()
		{
			LinkInvocation = ToolChain.MakeLinkInvocation(LinkUnit);
			return true;
		}

		private bool RunLinkInvocations()
		{
			if (!LinkInvocation.Run())
			{
				Log.Error($"{LinkInvocation.ProgramName} {LinkInvocation.Arguments.Join(" ")}");
				Log.Error($"Link {Module.TargetName} failed !");
				return false;
			}

			return true;
		}

		private NPath LinkResultPath()
		{
			var ex = Module.TargetBuildType == BuildType.Executable 
				? ToolChain.ExecutableExtension 
				: ToolChain.DynamicLibraryExtension;
			var prefix = Module.TargetBuildType == BuildType.Executable 
				? string.Empty
				: ToolChain.LibraryPrefix;
			return Source.OutputRoot
				.Combine(IPlatformSupport.CurrentTargetPlatform.ToString())
				.Combine(Options.Configuration.ToString())
				.Combine(Options.Architecture.Name)
				.Combine(prefix + Module.TargetName + ex);
		}
		
		#region LinkUnitInfo
		
		private IEnumerable<string> GetLinkFlagsForLinkUnit(CppLinkUnit unit)
		{
			foreach (var linkFlag in GetLinkFlagsForModule(Module))
			{
				yield return linkFlag;
			}

			foreach (var linkFlag in Options.CustomLinkFlags)
			{
				yield return linkFlag;
			}
		}

		private IEnumerable<string> GetStaticLibrariesForLinkUnit(CppLinkUnit unit)
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
		
		private IEnumerable<string> GetDynamicLibrariesForLinkUnit(CppLinkUnit unit)
		{
			foreach (var dynamicLibrary in GetDynamicLibrariesForModule(Module))
			{
				yield return dynamicLibrary;
			}

			foreach (var dynamicLibrary in Options.CustomDynamicLibraries)
			{
				yield return dynamicLibrary;
			}
		}

		private IEnumerable<NPath> GetLibrarySearchPathForLinkUnit(CppLinkUnit unit)
		{
			foreach (var dir in GetLibraryDirectoriesForModule(Module))
			{
				yield return dir.ToNPath();
			}

			foreach (var libraryDir in Options.CustomLibraryDirectories)
			{
				yield return libraryDir;
			}
		}
		
		#endregion
		
		private CppLinkUnit LinkUnit { get; set; } 

		private CppLinkInvocation LinkInvocation { get; set; }
		
	}
}