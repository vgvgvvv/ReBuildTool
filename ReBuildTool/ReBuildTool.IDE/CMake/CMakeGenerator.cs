using System.Text;

using NiceIO;
using ReBuildTool.Service.CompileService;
using ReBuildTool.Service.Global;
using ReBuildTool.Service.IDEService.CMake;
using ReBuildTool.ToolChain;

namespace ReBuildTool.IDE.CMake;

public class CMakeTarget
{
	public string TargetName;
	public BuildType TargetBuildType;
	
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
	
	public CMakeLists(IModuleInterface rule, NPath output)
	{
		Name = rule.TargetName;
		FullPath = output.Combine("CMakeLists.txt");
		InitWithRule(rule);
	}

	private void InitWithRule(IModuleInterface rule)
	{
		Targets.Add(TargetFromRule(rule));
	}

	private CMakeTarget TargetFromRule(IModuleInterface rule)
	{
		var target = new CMakeTarget();

		var cppBuilder = new CppBuilder();
		rule = cppBuilder.CompleteModuleInfo(rule);
		
		target.TargetName = rule.TargetName;
		target.TargetBuildType = rule.TargetBuildType;
		target.PublicIncludePaths.AddRange(rule.PublicIncludePaths.Select(p => p.ToNPath()));
		target.PrivateIncludePaths.AddRange(rule.PrivateIncludePaths.Select(p => p.ToNPath()));
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
		target.PrivateSources.AddRange(rule.SourceDirectories.SelectMany(dir => 
		{
			return dir.ToNPath().Files(true).Where(f => 
			{
				var ex = f.ExtensionWithDot;
				// TODO: replace with toolchain source file selection?
				if (ex == ".cpp" || ex == ".cxx" || ex == ".cc" || ex == ".c" || ex == ".inl")
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
	public ICMakeLists GenerateCMakeProject(ICppSourceProviderInterface source, NPath output)
	{
		Root.Version = "3.17";
		Root.ProjectName = Name;
		foreach ((string? key, var rule) in source.ModuleRules)
		{
			var moduleDirectory = output.Combine (rule.TargetName);
			var cmake = new CMakeLists(rule, moduleDirectory);
			ModuleCMakeLists.Add(cmake);
			Root.SubDirectories.Add(moduleDirectory);
		}
		return Root;
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
	private List<CMakeLists> ModuleCMakeLists = new List<CMakeLists>();
}