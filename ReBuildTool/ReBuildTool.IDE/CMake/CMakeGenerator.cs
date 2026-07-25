using System.Text;

using NiceIO;
using ReBuildTool.Common;
using ReBuildTool.Service.CompileService;
using ReBuildTool.Service.Global;
using ReBuildTool.Service.IDEService.CMake;
using ReBuildTool.ToolChain;

namespace ReBuildTool.IDE.CMake;

public class CMakeTarget
{
	public string TargetName;
	public BuildType TargetBuildType;

	// When true, the real add_library/add_executable target is emitted purely so the IDE
	// can read its sources/includes/defines/flags (IntelliSense + navigation), but it is
	// kept out of the default ALL build - the actual compilation is driven by rbt itself
	// through a custom target on the root project.
	public bool ExcludeFromAll { get; set; }

	public List<string> PublicDefines { get; } = new();
	public List<string> PrivateDefines { get; } = new();

	public List<string> PublicCompileFlags { get; } = new();
	public List<string> PrivateCompileFlags { get; } = new();
	
	public List<string> PublicLinkFlags { get; } = new();
	public List<string> PrivateLinkFlags { get; } = new();
	
	public List<string> PublicArchiveFlags { get; } = new();
	public List<string> PrivateArchiveFlags { get; } = new();
	
	public List<NPath> PublicIncludePaths { get; } = new ();
	public List<NPath> PrivateIncludePaths { get; } = new ();
	
	public List<string> PublicStaticLibraries { get; } = new ();
    
	public List<string> PrivateStaticLibraries { get; } = new ();
    
	public List<string> PublicDynamicLibraries { get; } = new ();
    
	public List<string> PrivateDynamicLibraries { get; } = new ();
	
	public List<NPath> PublicLibrarySearchPaths { get; } = new ();
	public List<NPath> PrivateLibrarySearchPaths { get; } = new ();

	public List<NPath> PublicSources { get; } = new();
	public List<NPath> PrivateSources { get; } = new();

	public List<string> Dependencies { get; } = new();

	public void FlushToBuilder(SourceCodeBuilder builder)
	{
		switch (TargetBuildType)
		{
		case BuildType.StaticLibrary:
			builder.AppendLine($"add_library({TargetName} STATIC)");
			break;
		case BuildType.DynamicLibrary:
			builder.AppendLine($"add_library({TargetName} SHARED)");
			break;
		case BuildType.Executable:
			builder.AppendLine($"add_executable({TargetName})");
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}

		if (ExcludeFromAll)
		{
			// Native CMake build is IntelliSense-only; the real build goes through rbt, so keep
			// this target out of ALL (otherwise `cmake --build` would compile it natively too).
			builder.AppendLine($"set_target_properties({TargetName} PROPERTIES EXCLUDE_FROM_ALL TRUE)");
		}

		#region definition

		{
			builder.AppendLine("target_compile_definitions(");
			builder.AppendLine(TargetName);
			builder.AppendLine("PUBLIC");
			foreach (string publicDefine in PublicDefines)
			{
				builder.AppendLine($"{publicDefine}");
			}
			builder.AppendLine("PRIVATE");
			foreach (string privateDefine in PrivateDefines)
			{
				builder.AppendLine($"{privateDefine}");
			}
			builder.AppendLine(")");
		}

		#endregion

		#region Source

		{
			builder.AppendLine("target_sources(");
			builder.AppendLine(TargetName);
			builder.AppendLine("PUBLIC");
			foreach (var publicSource in PublicSources)
			{
				builder.AppendLine($"{publicSource.InQuotes().Replace("\\", "/")}");
			}
			builder.AppendLine("PRIVATE");
			foreach (var privateSource in PrivateSources)
			{
				builder.AppendLine($"{privateSource.InQuotes().Replace("\\", "/")}");
			}
			builder.AppendLine(")");
		}

		#endregion

		#region Include

		{
			builder.AppendLine("target_include_directories(");
			builder.AppendLine(TargetName);
			builder.AppendLine("PUBLIC");
			foreach (var publicInclude in PublicIncludePaths)
			{
				builder.AppendLine($"{publicInclude.InQuotes().Replace("\\", "/")}");
			}
			builder.AppendLine("PRIVATE");
			foreach (var privateInclude in PrivateIncludePaths)
			{
				builder.AppendLine($"{privateInclude.InQuotes().Replace("\\", "/")}");
			}
			builder.AppendLine(")");
		}

		#endregion
		
		#region link library paths

		builder.AppendLine("target_link_directories(");
		builder.AppendLine(TargetName);
		builder.AppendLine("PUBLIC");
		foreach (var searchPath in PublicLibrarySearchPaths)
		{
			builder.AppendLine($"{searchPath.InQuotes().Replace("\\", "/")}");
		}
		builder.AppendLine("PRIVATE");
		foreach (var searchPath in PrivateLibrarySearchPaths)
		{
			builder.AppendLine($"{searchPath.InQuotes().Replace("\\", "/")}");
		}
		
		builder.AppendLine(")");

		#endregion

		#region link libraries

		builder.AppendLine("target_link_libraries(");
		builder.AppendLine(TargetName);
		builder.AppendLine("PUBLIC");
		foreach (string library in PublicDynamicLibraries)
		{
			builder.AppendLine(library);
		}
		foreach (string library in PublicStaticLibraries)
		{
			builder.AppendLine(library);
		}
		builder.AppendLine("PRIVATE");
		foreach (string library in PrivateDynamicLibraries)
		{
			builder.AppendLine(library);
		}
		foreach (string library in PrivateStaticLibraries)
		{
			builder.AppendLine(library);
		}

		builder.AppendLine(")");

		#endregion
		
		#region Compile flags

		builder.AppendLine("target_compile_options(");
		builder.AppendLine(TargetName);
		builder.AppendLine("PUBLIC");
		foreach (string compileFlag in PublicCompileFlags)
		{
			builder.AppendLine(compileFlag);
		}
		builder.AppendLine("PRIVATE");
		foreach (string compileFlag in PrivateCompileFlags)
		{
			builder.AppendLine(compileFlag);
		}
		builder.AppendLine(")");

		#endregion
		
		#region Link flags
		
		builder.AppendLine("target_link_options(");
		builder.AppendLine(TargetName);
		if (TargetBuildType != BuildType.StaticLibrary)
		{
			builder.AppendLine("PUBLIC");
			foreach (string linkFlag in PublicLinkFlags)
			{
				builder.AppendLine(linkFlag);
			}
			builder.AppendLine("PRIVATE");
			foreach (string linkFlag in PrivateLinkFlags)
			{
				builder.AppendLine(linkFlag);
			}
		}
		else
		{
			builder.AppendLine("PUBLIC");
			foreach (string linkFlag in PublicArchiveFlags)
			{
				builder.AppendLine(linkFlag);
			}
			builder.AppendLine("PRIVATE");
			foreach (string linkFlag in PrivateArchiveFlags)
			{
				builder.AppendLine(linkFlag);
			}
		}
		
		builder.AppendLine(")");

		
		#endregion

		#region dep
		
		builder.AppendLine("target_link_libraries(");
		builder.AppendLine(TargetName);
		builder.AppendLine("PUBLIC");
		foreach (string dependency in Dependencies)
		{
			builder.AppendLine(dependency);
		}
		builder.AppendLine(")");

		#endregion

	}
}

public class CMakeLists : ICMakeLists
{
	public string Name { get; }
	public NPath FullPath { get; }

	public CMakeLists(string name, NPath output)
	{
		Name = name;
		FullPath = output.Combine("CMakeLists.txt");
	}
	
	public CMakeLists(ICppSourceProviderInterface source, IModuleInterface rule, NPath output, bool excludeFromAll = false)
	{
		Name = rule.TargetName;
		FullPath = output.Combine("CMakeLists.txt");
		InitWithRule(source, rule, excludeFromAll);
	}

	private void InitWithRule(ICppSourceProviderInterface source, IModuleInterface rule, bool excludeFromAll)
	{
		var target = TargetFromRule(source, rule);
		if (target != null)
		{
			target.ExcludeFromAll = excludeFromAll;
			Targets.Add(target);
		}
	}

	private CMakeTarget? TargetFromRule(ICppSourceProviderInterface source, IModuleInterface rule)
	{
		var target = new CMakeTarget();

		var cppBuilder = new CppBuilder();
		cppBuilder.SetSource(source);
		rule = cppBuilder.CompleteModuleInfo(rule);
		if (rule == null)
		{
			return null;
		}
		
		target.TargetName = rule.TargetName;
		target.TargetBuildType = rule.TargetBuildType;
		target.PublicIncludePaths.AddRange(RulePathUtility.ExistingIncludePaths(
			rule,
			rule.PublicIncludePaths,
			"public include paths"));
		target.PrivateIncludePaths.AddRange(RulePathUtility.ExistingIncludePaths(
			rule,
			rule.PrivateIncludePaths,
			"private include paths"));
		target.PublicDefines.AddRange(rule.PublicDefines);
		target.PrivateDefines.AddRange(rule.PrivateDefines);
		target.PublicCompileFlags.AddRange(rule.PublicCompileFlags);
		target.PrivateCompileFlags.AddRange(rule.PrivateCompileFlags);
		target.PublicLinkFlags.AddRange(rule.PublicLinkFlags);
		target.PrivateLinkFlags.AddRange(rule.PrivateLinkFlags);
		target.PublicArchiveFlags.AddRange(rule.PublicArchiveFlags);
		target.PrivateArchiveFlags.AddRange(rule.PrivateArchiveFlags);
		target.PublicStaticLibraries.AddRange(rule.PublicStaticLibraries);
		target.PrivateStaticLibraries.AddRange(rule.PrivateStaticLibraries);
		target.PublicDynamicLibraries.AddRange(rule.PublicDynamicLibraries);
		target.PrivateDynamicLibraries.AddRange(rule.PrivateDynamicLibraries);
		target.PublicLibrarySearchPaths.AddRange(rule.PublicLibraryDirectories.Select(p=>p.ToNPath()));
		target.PrivateLibrarySearchPaths.AddRange(rule.PrivateLibraryDirectories.Select(p=>p.ToNPath()));
		target.Dependencies.AddRange(rule.Dependencies);
				target.PrivateSources.AddRange(RulePathUtility.ExistingSourceDirectories(
					rule,
					rule.SourceDirectories,
					"source directories").SelectMany(sourceRoot =>
				{
					var currentPlatform = IPlatformSupport.CurrentTargetPlatform.ToString();
					return sourceRoot.Files(true).Where(f =>
					{
						if (cppBuilder.CurrentToolChain.CanBeCompiled(f) && f.IsPlatformMatch(sourceRoot, currentPlatform))
						{
							return true;
						}
						return false;
					});
				}));
		return target;
	}

	public bool FlushToFile()
	{
		ToCMakeString();
		FullPath.EnsureParentDirectoryExists();
		FullPath.WriteAllText(builder.ToString());
		return true;
	}

	private void ToCMakeString()
	{
		if (!string.IsNullOrEmpty(Version))
		{
			builder.AppendLine($"cmake_minimum_required(VERSION {Version})");
		}
		if (!string.IsNullOrEmpty(ProjectName))
		{
			builder.AppendLine($"project({ProjectName})");
		}

		// Preamble runs before add_subdirectory (e.g. CMAKE_EXPORT_COMPILE_COMMANDS).
		builder.AppendLine(PreambleBuilder.ToString());

		foreach (var subDirectory in SubDirectories)
		{
			builder.AppendLine($"add_subdirectory({subDirectory.InQuotes().Replace("\\", "/")})");
		}

		builder.AppendLine(HeaderBuilder.ToString());

		foreach (var target in Targets)
		{
			target.FlushToBuilder(builder);
		}

		builder.AppendLine(FooterBuilder.ToString());
	}

	public string Version { get; set; }
	public string ProjectName { get; set; }
	public List<NPath> SubDirectories { get; } = new ();
	public List<CMakeTarget> Targets { get; } = new ();

	public StringBuilder PreambleBuilder { get; } = new();
	public StringBuilder HeaderBuilder { get; } = new();
	public StringBuilder FooterBuilder { get; } = new();
	
	SourceCodeBuilder builder = new SourceCodeBuilder();
}


public class CMakeGenerator : ICMakeGenerator
{
	
	public CMakeGenerator(string name, NPath outputPath)
	{
		Name = name;
		OutputPath = outputPath;
		outputPath.EnsureDirectoryExists();
		Root = new CMakeLists(Name, OutputPath.ToNPath());
	}
	
	public string Name { get; private set; }
	public string OutputPath { get; private set; }

	/// <summary>
	/// When true the generated CMake project does not compile natively: every module
	/// target is emitted for IDE IntelliSense/navigation only (EXCLUDE_FROM_ALL) while the
	/// real build is delegated to rbt through a custom target hooked to ALL.
	/// </summary>
	public bool UseRbtBuild { get; set; } = true;

	public ICMakeLists GenerateCMakeProject(ICppSourceProviderInterface source, NPath output)
	{
		Source = source;
		Root.Version = "3.17";
		Root.ProjectName = Name;

		if (UseRbtBuild)
		{
			// Emit compile_commands.json - this is what clangd / CLion / VS Code read for
			// hints and go-to-definition, and it does not require the native build to run.
			Root.PreambleBuilder.AppendLine("set(CMAKE_EXPORT_COMPILE_COMMANDS ON)");

			// Expose rbt's build configurations so the IDE's config selector maps straight onto
			// rbt's --BuildConfig via $<CONFIG> in the build target below. Multi-config generators
			// (VS, Ninja Multi-Config) pick the config at build time; single-config ones (Ninja,
			// Makefiles) at configure time via CMAKE_BUILD_TYPE - handle both.
			Root.PreambleBuilder.AppendLine("get_property(_rbtMultiConfig GLOBAL PROPERTY GENERATOR_IS_MULTI_CONFIG)");
			Root.PreambleBuilder.AppendLine("if(_rbtMultiConfig)");
			Root.PreambleBuilder.AppendLine("\tset(CMAKE_CONFIGURATION_TYPES \"Debug;Release;ReleasePlus;ReleaseSize\" CACHE STRING \"rbt build configurations\" FORCE)");
			Root.PreambleBuilder.AppendLine("else()");
			Root.PreambleBuilder.AppendLine("\tif(NOT CMAKE_BUILD_TYPE)");
			Root.PreambleBuilder.AppendLine("\t\tset(CMAKE_BUILD_TYPE \"Debug\" CACHE STRING \"rbt build configuration\" FORCE)");
			Root.PreambleBuilder.AppendLine("\tendif()");
			Root.PreambleBuilder.AppendLine("\tset_property(CACHE CMAKE_BUILD_TYPE PROPERTY STRINGS \"Debug\" \"Release\" \"ReleasePlus\" \"ReleaseSize\")");
			Root.PreambleBuilder.AppendLine("endif()");
		}

		foreach ((string? key, var rule) in source.ModuleRules)
		{
			var moduleDirectory = output.Combine (rule.TargetName);
			var cmake = new CMakeLists(source, rule, moduleDirectory, UseRbtBuild);
			ModuleCMakeLists.Add(cmake);
			Root.SubDirectories.Add(moduleDirectory);
		}

		if (UseRbtBuild)
		{
			AppendRbtBuildTarget(source);
		}

		return Root;
	}

	// The rbt executable currently generating this project - the CMake custom target invokes
	// the very same binary to run the real build, so it stays correct regardless of where rbt
	// lives (dev checkout vs Booster-installed) without depending on $RBT_HOME / the wrapper scripts.
	private static string RbtExecutablePath()
	{
		var exePath = Environment.ProcessPath;
		if (string.IsNullOrEmpty(exePath))
		{
			// Fallback for the rare case ProcessPath is unavailable (e.g. hosted via `dotnet <dll>`).
			exePath = System.Reflection.Assembly.GetEntryAssembly()?.Location;
		}
		return (exePath ?? string.Empty).Replace("\\", "/");
	}

	/// <summary>
	/// Emits a single root-level <c>add_custom_target(... ALL ...)</c> that invokes rbt to
	/// perform the real compilation. Because every module's add_library/add_executable is
	/// marked EXCLUDE_FROM_ALL, this is the only thing wired to ALL, so pressing "build" in
	/// the IDE (or running <c>cmake --build</c>) runs rbt's own toolchain and incremental build.
	/// </summary>
	private void AppendRbtBuildTarget(ICppSourceProviderInterface source)
	{
		var rbtExe = RbtExecutablePath();
		var projectRoot = source.ProjectRoot.ToString().Replace("\\", "/");
		var buildArgs = BuildRbtBuildArgs();

		Root.FooterBuilder.AppendLine($"add_custom_target({Name}_rbt_build ALL");
		Root.FooterBuilder.AppendLine($"\tCOMMAND \"{rbtExe}\" {buildArgs}");
		Root.FooterBuilder.AppendLine($"\tWORKING_DIRECTORY \"{projectRoot}\"");
		Root.FooterBuilder.AppendLine("\tUSES_TERMINAL");
		Root.FooterBuilder.AppendLine(")");
	}

	// The build must target the exact same platform/arch the project was generated for -
	// otherwise rbt falls back to the host platform (e.g. cross-generating for Android but then
	// building with the host MSVC toolchain, which fails on Android-only headers). Rather than
	// enumerate every relevant flag (TargetPlatform, TargetArch, NDK paths, ...), replay this
	// rbt process's own command line, since it already carries them all; only the run-mode,
	// IDE-generation and build-config flags are swapped out for a direct build.
	//
	// --BuildConfig is intentionally dropped from the replay and re-emitted as $<CONFIG>, so the
	// IDE's Debug/Release/... selector drives which config rbt builds (see the config block in
	// GenerateCMakeProject); CMake expands the generator expression per build.
	private static string BuildRbtBuildArgs()
	{
		// Flags (each followed by a value) that we override rather than pass through.
		// --UseNinjaBuild is dropped here for the same reason as --UseMakeFileBuild: the
		// CMake-driven build must go through the direct path (the IDE/cmake already
		// schedules the build), and a stale replay value could otherwise silently switch
		// the backend. Users wanting ninja can pass it explicitly when invoking the build.
		var overrideKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"--Mode", "--IDEProjectType", "--UseMakeFileBuild", "--UseNinjaBuild", "--BuildConfig"
		};

		var args = Environment.GetCommandLineArgs();
		var tokens = new List<string>();
		// args[0] is the executable path - skipped, RbtExecutablePath() is emitted separately.
		for (var i = 1; i < args.Length; i++)
		{
			if (overrideKeys.Contains(args[i]))
			{
				i++; // also skip the flag's value
				continue;
			}
			// Quote every token (paths may contain spaces) and use forward slashes so CMake does
			// not treat backslashes in the quoted string as escape sequences.
			tokens.Add($"\"{args[i].Replace("\\", "/")}\"");
		}

		tokens.Add("--Mode");
		tokens.Add("Build");
		tokens.Add("--UseMakeFileBuild");
		tokens.Add("false");
		// $<CONFIG> is a CMake generator expression expanded to the config being built, so the
		// IDE's Debug/Release/... selection is what rbt compiles.
		tokens.Add("--BuildConfig");
		tokens.Add("\"$<CONFIG>\"");
		return string.Join(' ', tokens);
	}

	public bool FlushAllCMakeFile()
	{
		foreach (var cmake in ModuleCMakeLists) 
		{
			if (!cmake.FlushToFile()) 
			{
				return false;
			}
		}
		return Root.FlushToFile();
	}

	private CMakeLists Root;
	private ICppSourceProviderInterface Source;
	private List<CMakeLists> ModuleCMakeLists = new List<CMakeLists>();
}
