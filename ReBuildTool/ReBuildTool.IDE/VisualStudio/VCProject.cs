using NiceIO;
using ReBuildTool.Service.Global;
using ReBuildTool.Service.CompileService;
using ReBuildTool.Service.IDEService.VisualStudio;
using ReBuildTool.ToolChain;

namespace ReBuildTool.IDE.VisualStudio;

public class VCProjectConfiguration : IProjectConfiguration
{
	public VCProjectConfiguration(Architecture arch, BuildConfiguration config)
	{
		Arch = arch;
		Configuration = config;
	}
	
	public string ConfigurationName => Configuration.ToString();
	public string PlatformName => Arch.Name;
	
	public Architecture Arch { get; }
	public BuildConfiguration Configuration { get; }
	
}

public partial class VCProject : ISlnSubProject
{
	private class Tags
	{
		public static string ProjectConfiguration = nameof(ProjectConfiguration);
		public static string Project = nameof(Project);
		public static string ItemGroup = nameof(ItemGroup);
		public static string PropertyGroup = nameof(PropertyGroup);
		public static string Filter = nameof(Filter);
		public static string ClCompile = nameof(ClCompile);
		public static string ClInclude = nameof(ClInclude);
		public static string None = nameof(None);
		public static string Import = nameof(Import);
	}
	
	public static VCProject GenerateOrGetVCProj(SlnGenerator owner, ICppSourceProviderInterface source, NPath output)
	{
		if (owner.GetSubProj(source.Name, out var subProject))
		{
			if (subProject is VCProject vcProj)
			{
				return vcProj;
			}

			throw new Exception("Project with same name already exists but is not a NetCoreCSProj");
		}

		VCProject result = new VCProject(owner, source, output);
		result.SetupAllModules();
		result.GenerateProject();
		result.GenerateFilter();
		result.GenerateCommonProps();
		owner.RegisterProj(result);
		return result;
	}

	/// <summary>
	/// Runs Setup() for every module before any project XML is generated, so that
	/// paths/defines/flags a module adds in its Setup() are visible to the per-file
	/// readers (GetIncludeDirectoriesForModule, GetOptionsForModule, ...).
	/// </summary>
	/// <remarks>
	/// Calls <see cref="CppModuleRule.Setup"/> directly instead of going through
	/// <see cref="CppBuilder.CompleteModuleInfo"/>/<see cref="CppModuleRule.SetupInternal"/>.
	/// The module instances live in the shared <see cref="cppSource.ModuleRules"/>
	/// dictionary and are reused by the build flow (CppBuilder.PendingModule). Going
	/// through SetupInternal would flip the module's <c>_hasSetup</c> flag here, so a
	/// later SetupInternal during Build would invoke Cleanup and wipe the
	/// SourceDirectories/IncludePaths that InitAllRule populated — leaving the build
	/// with no sources to compile. Calling Setup directly populates the lists for the
	/// IDE readers without arming the setup/cleanup lifecycle.
	/// </remarks>
	private void SetupAllModules()
	{
		var cppBuilder = new CppBuilder();
		cppBuilder.SetSource(cppSource);
		foreach (var (_, module) in cppSource.ModuleRules)
		{
			if (module is CppModuleRule cppModuleRule)
			{
				cppModuleRule.Setup(cppBuilder);
			}
		}
	}

	private VCProject(SlnGenerator owner, ICppSourceProviderInterface source, NPath output)
	{
		name = source.Name;
		guid = Guid.NewGuid();
		outputFolder = output;
		cppSource = source;
		outputFolder.EnsureDirectoryExists();
		projectCodeBuilder = new XmlCodeBuilder();
		filterCodeBuilder = new XmlCodeBuilder();
		commonPropBuilder = new XmlCodeBuilder();
		ownerSln = owner;
		generatorConfigProvider = new VCProjectConfigProvider();
	}

	private static List<IProjectConfiguration> _projectConfigs = null;
	public List<IProjectConfiguration> projectConfigs
	{
		get
		{
			if (_projectConfigs == null)
			{
				_projectConfigs = new List<IProjectConfiguration>();
				var targetArchs = new Architecture[]
					{ new x86Architecture(), new x64Architecture(), new ARMv7Architecture(), new ARM64Architecture() };
				var buildConfigs = new BuildConfiguration[]
					{ BuildConfiguration.Debug, BuildConfiguration.Release, BuildConfiguration.ReleasePlus, BuildConfiguration.ReleaseSize };
				foreach (var targetArch in targetArchs)
				{
					foreach (var buildConfiguration in buildConfigs)
					{
						_projectConfigs.Add(new VCProjectConfiguration(targetArch, buildConfiguration));
					}
				}
			}

			return _projectConfigs;
		}
	}

	public void FlushToFile()
	{
		File.WriteAllText(fullPath, projectCodeBuilder.ToString());
		File.WriteAllText(filterPath, filterCodeBuilder.ToString());
		File.WriteAllText(commonPropPath, commonPropBuilder.ToString());
	}

	private static bool IsHeader(NPath path)
	{
		return path.ExtensionWithDot == ".h" || 
		       path.ExtensionWithDot == "hpp" || 
		       path.ExtensionWithDot == "inl";
	}
		
	private static bool IsSource(NPath path)
	{
		return path.ExtensionWithDot == ".cpp" || 
		       path.ExtensionWithDot == "c" ||
		       path.ExtensionWithDot == "cc" ||
		       path.ExtensionWithDot == "cxx" ||
		       path.ExtensionWithDot == ".asm";
	}

	private static bool IsOther(NPath path)
	{
		return !IsHeader(path) && !IsSource(path);
	}
	
	public string name { get; private set; }
    public Guid guid { get; private set; }
    public NPath fullPath => outputFolder.Combine(name + ".vcxproj");
    public NPath filterPath => outputFolder.Combine(name + ".vcxproj.filters");
    public NPath commonPropPath => outputFolder.Combine(name + ".props");
	
	public SlnGenerator ownerSln { get; private set; }
	private ICppSourceProviderInterface cppSource;
	private NPath outputFolder;
	private XmlCodeBuilder projectCodeBuilder { get; }
	private XmlCodeBuilder filterCodeBuilder { get; }
	private XmlCodeBuilder commonPropBuilder { get; }

	private VCProjectConfigProvider generatorConfigProvider { get; }
}