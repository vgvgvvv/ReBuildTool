using NiceIO;
using ReBuildTool.Service.CompileService;
using ReBuildTool.Service.Global;
using ReBuildTool.Service.IDEService.VisualStudio;

namespace ReBuildTool.IDE.VisualStudio;

/// <summary>
/// A companion Makefile (.vcxproj) project emitted for an <see cref="BuildType.Executable"/>
/// module of a host <see cref="VCProject"/>. It reuses the host's rbt.bat build commands so it
/// builds the exact same way, but additionally ships a <c>.vcxproj.user</c> that points the VS
/// debugger (<c>LocalDebuggerCommand</c>) at the module's output binary, making the project
/// F5-launchable in Visual Studio.
/// </summary>
/// <remarks>
/// This project owns no sources of its own - it merely drives the same build as its host
/// <see cref="VCProject"/> through NMake and wires up the debugger. The .sln lists it alongside
/// the host project; VS records the actual "startup project" in the per-user .suo/.vs store.
/// </remarks>
public class LauncherVCProject : ISlnSubProject
{
	// VS doesn't auto-detect the installed MSVC toolset here - must match the host VCProject.
	private const string PlatformToolsetVersion = "v143";

	private class Tags
	{
		public static string Project = nameof(Project);
		public static string PropertyGroup = nameof(PropertyGroup);
		public static string ItemGroup = nameof(ItemGroup);
		public static string ProjectConfiguration = nameof(ProjectConfiguration);
		public static string Import = nameof(Import);
	}

	/// <summary>
	/// Returns the cached launcher for <paramref name="exeModule"/>, or constructs and registers
	/// a new one on the owning solution. Deduped by name (<c>{hostName}_{exeName}</c>).
	/// </summary>
	public static LauncherVCProject GenerateOrGetLauncher(
		SlnGenerator owner, VCProject host, IModuleInterface exeModule, NPath output)
	{
		var launcherName = host.name + "_" + exeModule.TargetName;
		if (owner.GetSubProj(launcherName, out var subProject))
		{
			if (subProject is LauncherVCProject launcher)
			{
				return launcher;
			}

			throw new Exception(
				$"Project with name '{launcherName}' already exists but is not a LauncherVCProject");
		}

		var result = new LauncherVCProject(owner, host, exeModule, output);
		result.GenerateProject();
		result.GenerateUserFile();
		owner.RegisterProj(result);
		return result;
	}

	private LauncherVCProject(SlnGenerator owner, VCProject host, IModuleInterface exeModule, NPath output)
	{
		name = host.name + "_" + exeModule.TargetName;
		guid = Guid.NewGuid();
		hostVCProject = host;
		executableModule = exeModule;
		outputFolder = output;
		outputFolder.EnsureDirectoryExists();
		projectCodeBuilder = new XmlCodeBuilder();
		userCodeBuilder = new XmlCodeBuilder();
		ownerSln = owner;
	}

	public string name { get; }
	public Guid guid { get; }
	public NPath fullPath => outputFolder.Combine(name + ".vcxproj");
	public NPath userPath => outputFolder.Combine(name + ".vcxproj.user");

	// Reuse the host project's config matrix (4 archs x 4 configs).
	public List<IProjectConfiguration> projectConfigs => hostVCProject.projectConfigs;

	public void FlushToFile()
	{
		File.WriteAllText(fullPath, projectCodeBuilder.ToString());
		File.WriteAllText(userPath, userCodeBuilder.ToString());
	}

	public SlnGenerator ownerSln { get; }

	// --- vcxproj generation (Makefile layout, mirrors the host VCProject) ---

	private void GenerateProject()
	{
		projectCodeBuilder.Builder.Clear();
		projectCodeBuilder.WriteHeader();
		using (projectCodeBuilder.CreateXmlScope(Tags.Project,
			       new Tuple<string, string>("DefaultTargets", "Build"),
			       new Tuple<string, string>("ToolsVersion", "17.0"),
			       new Tuple<string, string>("xmlns", "http://schemas.microsoft.com/developer/msbuild/2003")))
		{
			GenerateProjectConfigurationList();
			GenerateGlobalProperty();
			GenerateImportDefaultProps();
			// ConfigurationType must be declared before Microsoft.Cpp.props is imported, otherwise
			// VS evaluates it as a normal (non-Makefile) project and ignores all NMake* settings.
			GenerateConfigurationTypePropertyGroups();
			GenerateImportCppProps();
			GenerateNMakePropertyGroups();
			GenerateImportCppTargets();
		}
	}

	private void GenerateProjectConfigurationList()
	{
		using (projectCodeBuilder.CreateXmlScope(Tags.ItemGroup,
			       new Tuple<string, string>("Label", "ProjectConfigurations")))
		{
			foreach (var configuration in projectConfigs)
			{
				using (projectCodeBuilder.CreateXmlScope(Tags.ProjectConfiguration,
					       new Tuple<string, string>("Include",
						       $"{configuration.ConfigurationName}|{configuration.PlatformName}")))
				{
					projectCodeBuilder.WriteNode("Configuration", configuration.ConfigurationName);
					projectCodeBuilder.WriteNode("Platform", configuration.PlatformName);
				}
			}
		}
	}

	private void GenerateGlobalProperty()
	{
		using (projectCodeBuilder.CreateXmlScope(Tags.PropertyGroup,
			       new Tuple<string, string>("Label", "Globals")))
		{
			projectCodeBuilder.WriteNode("ProjectGuid", guid.ToString());
		}
	}

	private void GenerateImportDefaultProps()
	{
		projectCodeBuilder.WriteNodeWithoutValue(Tags.Import,
			new Tuple<string, string>("Project", @"$(VCTargetsPath)\Microsoft.Cpp.Default.props"));
	}

	private void GenerateConfigurationTypePropertyGroups()
	{
		foreach (var configuration in projectConfigs)
		{
			using (projectCodeBuilder.CreateXmlScope(Tags.PropertyGroup,
				       new Tuple<string, string>("Label", "Configuration"),
				       new Tuple<string, string>("Condition",
					       $"'$(Configuration)|$(Platform)'=='{configuration.ConfigurationName}|{configuration.PlatformName}'")))
			{
				projectCodeBuilder.WriteNode("ConfigurationType", "Makefile");
				projectCodeBuilder.WriteNode("PlatformToolset", PlatformToolsetVersion);
			}
		}
	}

	private void GenerateImportCppProps()
	{
		// Inline the Microsoft.Cpp.props import (the host VCProject does this via a shared .props
		// file; a launcher has nothing to share, so import directly).
		projectCodeBuilder.WriteNodeWithoutValue(Tags.Import,
			new Tuple<string, string>("Project", @"$(VCTargetsPath)\Microsoft.Cpp.props"));
	}

	private void GenerateImportCppTargets()
	{
		projectCodeBuilder.WriteNodeWithoutValue(Tags.Import,
			new Tuple<string, string>("Project", @"$(VCTargetsPath)\Microsoft.Cpp.targets"));
	}

	private void GenerateNMakePropertyGroups()
	{
		foreach (var configuration in projectConfigs)
		{
			GenerateNMakePropertyGroup(configuration);
		}
	}

	private void GenerateNMakePropertyGroup(IProjectConfiguration configuration)
	{
		using (projectCodeBuilder.CreateXmlScope("PropertyGroup",
			       new Tuple<string, string>("Condition",
				       $"'$(Configuration)|$(Platform)'=='{configuration.ConfigurationName}|{configuration.PlatformName}'")))
		{
			// Same build/rebuild/clean commands as the host project - building the launcher
			// builds the whole target through rbt.bat, exactly like the host Makefile project.
			var commonArgs = hostVCProject.BuildCommonNMakeArgs(configuration);
			projectCodeBuilder.WriteNode("NMakeBuildCommandLine", $"{commonArgs} --Mode Build");
			projectCodeBuilder.WriteNode("NMakeReBuildCommandLine", $"{commonArgs} --Mode ReBuild");
			projectCodeBuilder.WriteNode("NMakeCleanCommandLine", $"{commonArgs} --Mode Clean");
			projectCodeBuilder.WriteNode("NMakeOutput", GetExecutableOutput(configuration));
		}
	}

	// --- .vcxproj.user generation (wires up the VS debugger to the exe) ---

	private void GenerateUserFile()
	{
		userCodeBuilder.Builder.Clear();
		userCodeBuilder.WriteHeader();
		using (userCodeBuilder.CreateXmlScope(Tags.Project,
			       new Tuple<string, string>("ToolsVersion", "14.0"),
			       new Tuple<string, string>("xmlns", "http://schemas.microsoft.com/developer/msbuild/2003")))
		{
			foreach (var configuration in projectConfigs)
			{
				GenerateUserPropertyGroup(configuration);
			}
		}
	}

	private void GenerateUserPropertyGroup(IProjectConfiguration configuration)
	{
		using (userCodeBuilder.CreateXmlScope(Tags.PropertyGroup,
			       new Tuple<string, string>("Label", "Configuration"),
			       new Tuple<string, string>("Condition",
				       $"'$(Configuration)|$(Platform)'=='{configuration.ConfigurationName}|{configuration.PlatformName}'")))
		{
			var exePath = GetExecutableOutput(configuration);
			// Use the exe's directory so relative runtime assets resolve correctly when debugging.
			var workingDir = string.IsNullOrEmpty(exePath)
				? string.Empty
				: new NPath(exePath).Parent.ToString();

			userCodeBuilder.WriteNode("LocalDebuggerCommand", exePath);
			userCodeBuilder.WriteNode("LocalDebuggerWorkingDirectory", workingDir);
			// Selects the native Windows debugger engine in the "Debugger to launch" dropdown.
			// Must be the exact enum string VS persists (WindowsLocalDebugger), otherwise the
			// dropdown stays unset and the project isn't F5-launchable until set by hand.
			userCodeBuilder.WriteNode("DebuggerFlavor", "WindowsLocalDebugger");
		}
	}

	/// <summary>
	/// Resolves the executable module's output path for a configuration, delegating to the host
	/// project's path algorithm (which accounts for platform/config/arch and the toolchain's
	/// ExecutableExtension).
	/// </summary>
	private string GetExecutableOutput(IProjectConfiguration configuration)
	{
		return hostVCProject.GetExecutableOutputPath(executableModule, configuration);
	}

	private VCProject hostVCProject { get; }
	private IModuleInterface executableModule { get; }
	private NPath outputFolder { get; }
	private XmlCodeBuilder projectCodeBuilder { get; }
	private XmlCodeBuilder userCodeBuilder { get; }
}
