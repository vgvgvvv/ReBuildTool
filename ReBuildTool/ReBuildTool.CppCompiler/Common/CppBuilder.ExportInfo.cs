using ReBuildTool.Service.CompileService;

namespace ReBuildTool.ToolChain;

public partial class CppBuilder
{

	private class ExportModuleInfo : IModuleInterface
	{
		public BuildType TargetBuildType { get; set; }
		public string TargetName { get; set; }
		public List<string> PublicIncludePaths { get; } = new();
		public List<string> PrivateIncludePaths { get; } = new();
		public List<string> PublicDefines { get; } = new();
		public List<string> PrivateDefines { get; } = new();
		public List<string> PublicCompileFlags { get; } = new();
		public List<string> PrivateCompileFlags { get; } = new();
		public List<string> PublicLinkFlags { get; } = new();
		public List<string> PrivateLinkFlags { get; } = new();
		public List<string> PublicArchiveFlags { get; } = new();
		public List<string> PrivateArchiveFlags { get; } = new();
		public List<string> PublicStaticLibraries { get; } = new();
		public List<string> PrivateStaticLibraries { get; } = new();
		public List<string> PublicDynamicLibraries { get; } = new();
		public List<string> PrivateDynamicLibraries { get; } = new();
		public List<string> PublicLibraryDirectories { get; } = new();
		public List<string> PrivateLibraryDirectories { get; } = new();
		public List<string> SourceDirectories { get; } = new();
		public List<string> Dependencies { get; } = new();
		public string ModuleDirectory { get; set; }
		public bool IsSupport { get; set; } = true;
	}
	
	public IModuleInterface? CompleteModuleInfo(IModuleInterface targetRule)
	{
		if (targetRule is CppModuleRule cppModuleRule)
		{
			cppModuleRule.SetupInternal(this);
		}

		if (!targetRule.IsSupport)
		{
			return null;
		}
		
		var exportInfo = new ExportModuleInfo();
		CompileProcess process = CompileProcess.Create(targetRule, this);
		exportInfo.TargetBuildType = targetRule.TargetBuildType;
		exportInfo.TargetName = targetRule.TargetName;
		
		exportInfo.PublicIncludePaths.AddRange(targetRule.PublicIncludePaths);
		exportInfo.PrivateIncludePaths.AddRange(process.GetIncludePathsForModule(targetRule));
		exportInfo.PrivateIncludePaths.AddRange(CurrentToolChain.ToolChainIncludePaths().Select(p => p.ToString()));
		
		exportInfo.PublicDefines.AddRange(targetRule.PublicDefines);
		exportInfo.PrivateDefines.AddRange(process.GetDefinesForModule(targetRule));
		exportInfo.PrivateDefines.AddRange(CurrentToolChain.ToolChainDefines());
		
		exportInfo.PublicCompileFlags.AddRange(targetRule.PublicCompileFlags);
		exportInfo.PrivateCompileFlags.AddRange(process.GetCompileFlagsForModule(targetRule));
		
		exportInfo.PublicLinkFlags.AddRange(targetRule.PublicLinkFlags);
		exportInfo.PrivateLinkFlags.AddRange(process.GetLinkFlagsForModule(targetRule));
		
		exportInfo.PublicArchiveFlags.AddRange(targetRule.PublicArchiveFlags);
		exportInfo.PrivateArchiveFlags.AddRange(process.GetArchiveFlagsForModule(targetRule));
		
		exportInfo.PublicStaticLibraries.AddRange(targetRule.PublicStaticLibraries);
		exportInfo.PrivateStaticLibraries.AddRange(process.GetStaticLibrariesForModule(targetRule));
		exportInfo.PrivateStaticLibraries.AddRange(CurrentToolChain.ToolChainStaticLibraries());
		
		exportInfo.PublicDynamicLibraries.AddRange(targetRule.PublicDynamicLibraries);
		exportInfo.PrivateDynamicLibraries.AddRange(process.GetDynamicLibrariesForModule(targetRule));
		exportInfo.PrivateDynamicLibraries.AddRange(CurrentToolChain.ToolChainDynamicLibraries());
		
		exportInfo.PublicLibraryDirectories.AddRange(targetRule.PublicLibraryDirectories);
		exportInfo.PrivateLibraryDirectories.AddRange(process.GetLibraryDirectoriesForModule(targetRule));
		exportInfo.PrivateLibraryDirectories.AddRange(CurrentToolChain.ToolChainLibraryPaths().Select(p => p.ToString()));
		
		exportInfo.SourceDirectories.AddRange(targetRule.SourceDirectories);
		exportInfo.Dependencies.AddRange(targetRule.Dependencies);
		exportInfo.ModuleDirectory = targetRule.ModuleDirectory;
		exportInfo.IsSupport = targetRule.IsSupport;
		return exportInfo;
	}
}
