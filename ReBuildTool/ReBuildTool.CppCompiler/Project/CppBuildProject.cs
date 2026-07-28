using System.Reflection;
using NiceIO;
using ReBuildTool.CppCompiler;
using ReBuildTool.Service.CompileService;
using ReBuildTool.Service.Context;
using ReBuildTool.Service.Global;
using ReBuildTool.Service.IDEService;
using ReBuildTool.Service.IDEService.CMake;
using ReBuildTool.Service.IDEService.VisualStudio;
using ReBuildTool.Service.PackageService;
using ReBuildTool.ToolChain.Package;
using ResetCore.Common;
using ResetCore.Common.Parser.Ini;

namespace ReBuildTool.ToolChain.Project;

public interface ICppSourceProvider : ICppSourceProviderInterface
{
	string Name { get; }
	NPath ProjectRoot { get; }
	NPath SourceFolder { get; }
	NPath IntermediaFolder { get; }
	Dictionary<string, ITargetInterface> TargetRules { get; }
	Dictionary<string, IModuleInterface> ModuleRules { get; }
}

public class CppBuildProject : ICppSourceProvider, ICppProject
{

	
	private CppBuildProject(NPath workDirectory)
	{
		Name = workDirectory.FileName;
		ProjectRoot = workDirectory;
		InitAllCompilePlugin();
	}

	private void ParseRules()
	{
		var targetFiles = SourceFolder.Files($"*{ICppProject.TargetDefineExtension}", true).ToList();
		var moduleFiles = SourceFolder.Files($"*{ICppProject.ModuleDefineExtension}", true).ToList();
		var extraFiles = SourceFolder.Files($"*{ICppProject.ExtensionDefineExtension}", true).ToList();

		// Only the project's own Source/ decides whether this is a fresh project needing a
		// scaffold. A project that consumes packages but has not written its target yet still
		// has to be initialized, so restored packages must not count here.
		if (targetFiles.Count == 0)
		{
			CreateDefaultProject();
			ParseRules();
			return;
		}

		// Packages contribute modules, never targets: what to build is the consuming project's
		// decision, and a package's target would otherwise silently join the build.
		foreach (var root in PackageRuleRoots)
		{
			moduleFiles.AddRange(root.Files($"*{ICppProject.ModuleDefineExtension}", true));
			extraFiles.AddRange(root.Files($"*{ICppProject.ExtensionDefineExtension}", true));
		}

		foreach (var targetFile in targetFiles)
		{
			TargetRulePaths.Add(targetFile.FileNameWithoutExtension, targetFile);
		}
		
		foreach (var moduleFile in moduleFiles)
		{
			var fileName = moduleFile.FileName;
			var moduleName = fileName.Substring(0, fileName.Length - ICppProject.ModuleDefineExtension.Length);
			// Two rule files claiming one module name cannot both win, and the rule assembly would
			// fail to compile later with a duplicate-type error that names neither file. Packages
			// make this collision far more likely, so say exactly which files clash.
			if (ModuleRulePaths.TryGetValue(moduleName, out var existing))
			{
				throw new Exception(
					$"module \"{moduleName}\" is defined twice:{Environment.NewLine}" +
					$"  {existing}{Environment.NewLine}" +
					$"  {moduleFile}");
			}
			ModuleRulePaths.Add(moduleName, moduleFile);
		}
		
		var compiler = ServiceContext.Instance.FindService<ICSharpCompilerService>().Value;
		
		BuildRuleCompileUnit = compiler.CreateAssemblyUnit();
		BuildRuleCompileUnit.SourceFiles.AddRange(targetFiles);
		BuildRuleCompileUnit.SourceFiles.AddRange(moduleFiles);
		BuildRuleCompileUnit.SourceFiles.AddRange(extraFiles);
		BuildRuleCompileUnit.ReferenceDlls.Add(Assembly.GetAssembly(typeof(CppBuildProject))!.Location.ToNPath());
		BuildRuleCompileUnit.ReferenceDlls.Add(Assembly.GetAssembly(typeof(IModuleInterface))!.Location.ToNPath());
		BuildRuleCompileUnit.ReferenceDlls.Add(Assembly.GetAssembly(typeof(PlatformHelper))!.Location.ToNPath());
		BuildRuleCompileUnit.FileName = "CompileRules";
	}

	private void CreateDefaultProject()
	{
		SourceFolder.EnsureDirectoryExists();
		var targetName = GlobalCmd.CommonCommand.Target.Value;
		if (string.IsNullOrEmpty(targetName))
		{
			targetName = ProjectRoot.FileName;
		}

		{
			var defaultTargetContent = @"using ReBuildTool.ToolChain;

public class ${targetName}Target : CppTargetRule
{
    public ${targetName}Target()
    {
        UsedModules.Add(""${targetName}Module"");
    }
} 
";
			ContextArgs.Context context = new ContextArgs.Context();
			context.AddArg("targetName", targetName);
			ContextArgs text = new ContextArgs(defaultTargetContent);
			File.WriteAllText(SourceFolder.Combine($"{targetName}Target{ICppProject.TargetDefineExtension}"), text.GetText(context));
		}

		var moduleFolder = SourceFolder.Combine($"Src/{targetName}").CreateDirectory();
		var moduleName = $"{targetName}Module";
		{
			// Scaffolds the Setup() shape, not a constructor: a module rule must declare
			// from Setup(ICppBuildContext) - see CppModuleRule.ThrowIfDeclaredInConstructor.
			var defaultTargetContent = @"using ReBuildTool.ToolChain;

public class ${targetName}Module : CppModuleRule
{
    public override void Setup(ICppBuildContext buildContext)
    {
        // TargetBuildType = BuildType.Executable;
        // Dependencies.Add(""SomeOtherModule"");
    }
}
";
			ContextArgs.Context context = new ContextArgs.Context();
			context.AddArg("targetName", targetName);
			ContextArgs text = new ContextArgs(defaultTargetContent);
			File.WriteAllText(moduleFolder.Combine($"{moduleName}{ICppProject.ModuleDefineExtension}"), text.GetText(context));

		}

		{
			var privateSourceFolder = moduleFolder.Combine("Private").CreateDirectory();
			{
				var moduleSourceContent = @"#include ""${moduleName}.h""

${moduleName}::${moduleName}()
{
}

${moduleName}::~${moduleName}()
{
}
";
				ContextArgs.Context context = new ContextArgs.Context();
				context.AddArg("moduleName", moduleName);
				ContextArgs text = new ContextArgs(moduleSourceContent);
				File.WriteAllText(privateSourceFolder.Combine($"{moduleName}.cpp"), text.GetText(context));
			}

			var publicSourceFolder = moduleFolder.Combine("Public").CreateDirectory();
			{
				var moduleHeaderContent = @"#pragma once

class ${moduleName}
{
public:
    ${moduleName}();
    ~${moduleName}();
};
";
				ContextArgs.Context context = new ContextArgs.Context();
				context.AddArg("moduleName", moduleName);
				ContextArgs text = new ContextArgs(moduleHeaderContent);
				File.WriteAllText(publicSourceFolder.Combine($"{moduleName}.h"), text.GetText(context));
			}
			
		}
		
	}

	public void Parse()
	{
		Restore();
		ParseRules();
	}

	/// <summary>
	/// Brings the declared packages onto disk. This has to happen before <see cref="ParseRules"/>:
	/// a package's own <c>.module.cs</c> files are globbed into the same rule assembly as the
	/// project's, and that assembly is loaded once with <c>Assembly.LoadFile</c> and can never be
	/// unloaded - there is no second chance to add rules after the fact.
	///
	/// A project without an <c>RBTPackage.json</c> pays nothing: the service returns immediately
	/// and no Packages/ directory or lock file is created.
	/// </summary>
	public void Restore()
	{
		var service = ServiceContext.Instance.FindService<IPackageService>();
		if (!service)
		{
			// Package support is optional; a context that did not register it still builds.
			return;
		}

		var packageArgs = PackageArgs.Get();
		// Manifest edits happen before resolution, so --PackageAdd both records the dependency and
		// fetches it in one invocation.
		if (packageArgs.PackageAdd.IsSet && !string.IsNullOrWhiteSpace(packageArgs.PackageAdd.Value))
		{
			PackageManifestEditor.Add(ProjectRoot, packageArgs.PackageAdd.Value);
		}
		if (packageArgs.PackageRemove.IsSet && !string.IsNullOrWhiteSpace(packageArgs.PackageRemove.Value))
		{
			PackageManifestEditor.Remove(ProjectRoot, packageArgs.PackageRemove.Value);
		}

		var result = service.Value.Restore(ProjectRoot, packageArgs.ToRestoreOptions());
		RestoredPackages.Clear();
		RestoredPackages.AddRange(result.Packages);

		PackageRuleRoots.Clear();
		PackageRuleRoots.AddRange(RestoredPackages.Select(package => package.Root));
		if (RestoredPackages.Count > 0)
		{
			// Packages that ship prebuilt binaries (or upstream sources with no rule of their own)
			// have their module rule synthesized here, into extra directories that get globbed
			// alongside the packages themselves.
			PackageRuleRoots.AddRange(PackageModuleBinder.Bind(
				ProjectRoot.Combine(PackageRestoreService.PackagesFolderName),
				RestoredPackages));
		}
	}

	public void Setup()
	{
		// generate dll && do init functions
		InitAllRule();
		GenerateCppProject();
	}

	public void GenerateCppProject()
	{
		InitAllRule();

		var result = ServiceContext.Instance.Create<IGenerateIDEProjService>();
		if (result.IsFailed)
		{
			throw new NullReferenceException($"can not create IGenerateIDEProjService {result.Error}");
		}
		var gener = result.Value;
		
		gener.GenerateRuleSln(ProjectRoot, BuildRuleCompileUnit, CppBuildRuleProjectOutput);
		gener.Generate(Name, this, ProjectRoot, CppProjectOutput);
	}

	public void Build(string? targetName = null)
	{
		Build(targetName, null);
	}
	
	public void Build(string? targetName, IBuildConfigProvider? configProvider = null)
	{
		CleanIfNeed();
		
		InitAllRule();
		var builder = new CppBuilder(configProvider);
		
		try
		{
			PreCompile(builder);
			
			// do build & build hooks	
			if (targetName == null)
			{
				BuildAll(builder);
				return;
			}

			if (!TargetRules.TryGetValue(targetName, out var targetRule))
			{
				BuildAll(builder);
				return;
			}
		
			Build(builder, targetRule);
		}
		finally
		{
			PostCompile(builder);
		}
		
	}

	public void Clean()
	{
		OutputRoot.DeleteIfExists(DeleteMode.Normal);
		// Delete IntermediaFolder's contents but preserve the Logs/ subdirectory:
		// the process holds Intermedia/Logs/Build.log open via FileLogger for its
		// whole lifetime (Program.cs opens it before dispatching to Clean/Build,
		// closing only at process exit), so a blanket DeleteIfExists would hit
		// "file in use" on Windows and abort the clean. Logs/ is also what the
		// user reads to see what just happened, so keeping it across cleans is
		// desirable beyond just dodging the handle.
		if (IntermediaFolder.DirectoryExists())
		{
			foreach (var child in IntermediaFolder.Contents())
			{
				if (string.Equals(child.FileName, "Logs", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				child.DeleteIfExists(DeleteMode.Normal);
			}
		}
	}

	private void CleanIfNeed()
	{
		var dllNewestTime = Assembly.GetEntryAssembly().Location
			.ToNPath().Parent.Files()
			.Select(file => File.GetLastWriteTimeUtc(file))
			.Max();

		var timeStampFile = IntermediaFolder.Combine("LastBuildToolTimeStamp");
		if (timeStampFile.Exists())
		{
			var dateTime = DateTime.Parse(timeStampFile.ReadAllText());
			if (dllNewestTime - dateTime > TimeSpan.FromSeconds(1))
			{
				Log.Warning($"build tool has been updated({dateTime} -> {dllNewestTime}), clean all");
				Clean();
				timeStampFile.EnsureParentDirectoryExists();
				timeStampFile.WriteAllText(dllNewestTime.ToLongTimeString());
			}
		}
	}

	public void ReBuild(string? targetName = null)
	{
		Clean();
		Build(targetName);
	}
	
#region Setup

	private bool NeedReBuildRuleAssembly()
	{
		if (!CppBuildRuleDllPath.Exists())
		{
			return true;
		}
		var lastBuildTime = File.GetLastWriteTime(CppBuildRuleDllPath);
		foreach (var sourceFile in BuildRuleCompileUnit.SourceFiles)
		{
			var fileTime = File.GetLastWriteTime(sourceFile);
			if(fileTime > lastBuildTime)
			{
				return true;
			}
		}
		return false;
	}

	private void BuildRuleAssembly()
	{
		var compiler = ServiceContext.Instance.FindService<ICSharpCompilerService>();
		if (!compiler)
		{
			throw new Exception("cannot find compiler !!");
		}
		compiler.Value.Compile(CppBuildRuleBinaryOutput, new List<IAssemblyCompileUnit>()
		{
			BuildRuleCompileUnit
		}, compiler.Value.DefaultEnvironment);
	}

	private static int RetryCompileRuleTimes = 0;
	private void InitAllRule()
	{
		if (NeedReBuildRuleAssembly())
		{
			BuildRuleAssembly();
		}
		
		if (TargetRules.Count != 0 || ModuleRules.Count != 0)
		{
			return;
		}
		Assembly compileRuleAssembly = null;
		bool loadAssemblySucc = true;
		try
		{
			compileRuleAssembly = Assembly.LoadFile(CppBuildRuleDllPath);
		} 
		catch (Exception e)
		{
			loadAssemblySucc = false;
		} 
		finally
		{
			if (compileRuleAssembly == null)
			{
				loadAssemblySucc = false;
			}
		}
		
		if (!loadAssemblySucc)
		{
			RetryCompileRuleTimes++;
			if (RetryCompileRuleTimes > 3)
			{
				throw new Exception("Compile Rule Assembly Failed");
			}
			BuildRuleAssembly();
			InitAllRule();
			return;
		}
		RetryCompileRuleTimes = 0;

		var targetRules = compileRuleAssembly.GetTypes()
			.Where(t => t.IsSubclassOf(typeof(CppTargetRule)) && !t.IsGenericType && !t.IsAbstract)
			.Select(t => Activator.CreateInstance(t) as CppTargetRule)
			.ToList();
		foreach (var rule in targetRules)
		{
			if (rule == null)
			{
				continue;
			}
			var ruleName = rule.GetType().Name;
			if (TargetRulePaths.TryGetValue(ruleName, out var targetRulePath))
			{
				rule.TargetDirectory = targetRulePath.Parent;
			}
			TargetRules.Add(ruleName, rule);
		}
		
		var moduleRules = compileRuleAssembly.GetTypes()
			.Where(t => t.IsSubclassOf(typeof(CppModuleRule)) && !t.IsGenericType && !t.IsAbstract)
			.Select(t => Activator.CreateInstance(t) as CppModuleRule)
			.ToList();
		
		foreach (var rule in moduleRules)
		{
			if (rule == null)
			{
				continue;
			}

			var ruleName = rule.GetType().Name;
			if (ModuleRulePaths.TryGetValue(ruleName, out var moduleRulePath))
			{
				// Before anything is injected: a rule that declared from its constructor
				// would lose those entries on the next setup pass, so say so now rather
				// than let it fail as a missing header or an empty link.
				rule.ThrowIfDeclaredInConstructor();

				GenerateModuleCodes(rule);
				rule.ModuleDirectory = moduleRulePath.Parent;
				// Registered as framework paths: the live lists are emptied by Cleanup on
				// every setup pass, these are put back each time.
				rule.AddFrameworkSourceDirectory(moduleRulePath.Parent.Combine("Public"));
				rule.AddFrameworkSourceDirectory(moduleRulePath.Parent.Combine("Private"));
				rule.AddFrameworkPublicIncludePath(moduleRulePath.Parent.Combine("Public"));
				rule.AddFrameworkPrivateIncludePath(moduleRulePath.Parent.Combine("Private"));
			}
			else
			{
				throw new Exception($"cannot find module {ruleName}");
			}
			ModuleRules.Add(ruleName, rule);
		}

		var plugins = TargetRules.SelectMany(rule => rule.Value.Plugins).ToList();
		foreach (var targetPlugin in plugins)
		{
			targetPlugin.Setup(this);
		}
	}

	private void GenerateModuleCodes(IModuleInterface module)
	{
		CppModuleRule.GenerateCode(module, IntermediaFolder);
	}

#endregion

#region Build

	private void Build(CppBuilder builder, ITargetInterface targetRule)
	{
		builder.SetSource(this);
		if (targetRule is CppTargetRule cppTargetRule)
		{
			cppTargetRule.Setup(builder);
			
			builder.CalculateDepModules(targetRule);
			
			foreach (var compilePlugin in cppTargetRule.Plugins)
			{
				if (compilePlugin is BaseCppTargetCompilePlugin cppTargetPlugin)
				{
					cppTargetPlugin.Setup(this);
				}
			}
			
			foreach (var compilePlugin in cppTargetRule.Plugins)
			{
				if (compilePlugin is BaseCppTargetCompilePlugin cppTargetPlugin)
				{
					cppTargetPlugin.PreCompile(cppTargetRule, builder);
				}
			}
			
			builder.BuildTarget(targetRule);
			foreach (var compilePlugin in cppTargetRule.Plugins)
			{
				if (compilePlugin is BaseCppTargetCompilePlugin cppTargetPlugin)
				{
					cppTargetPlugin.PostCompile(cppTargetRule, builder);
				}
			}
		}
		else
		{
			builder.CalculateDepModules(targetRule);
			builder.BuildTarget(targetRule);
		}

		if (targetRule is IPostBuildTarget postBuildTarget)
		{
			postBuildTarget.PostBuild();
		}
		
	}
	
	private void BuildAll(CppBuilder builder)
	{
		foreach (var (key, targetRule) in TargetRules)
		{
			Build(builder, targetRule);
		}
	}

#endregion

#region Compile Plugin

	private void InitAllCompilePlugin()
	{
		var compilerArgs = CppCompilerArgs.Get();
		if (!compilerArgs.CppCompilePlugins.IsSet)
		{
			return;
		}
		var pluginTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(asm => asm.GetTypes()
			.Where(t => t.IsSubclassOf(typeof(BaseCppCompilePlugin)) 
			            && !t.IsAbstract 
			            && compilerArgs.CppCompilePlugins.Value.Contains(t.Name)))
			.ToList();
		foreach (var pluginType in pluginTypes)
		{
			var plugin = Activator.CreateInstance(pluginType) as BaseCppCompilePlugin;
			if (plugin == null)
			{
				Log.Error($"create plugin {pluginType.FullName} failed");
				continue;
			}
			CppCompilePlugins.Add(plugin);
		}
	}

	private void PreCompile(CppBuilder builder)
	{
		foreach (var plugin in CppCompilePlugins)
		{
			plugin.PreCompile(builder);
		}
	}
	
	private void PostCompile(CppBuilder builder)
	{
		foreach (var plugin in CppCompilePlugins)
		{
			plugin.PostCompile(builder);
		}
	}

#endregion

	public string Name { get; }
	public NPath ProjectRoot { get; }
	public NPath OutputRoot => ProjectRoot.Combine("Binary");

	public NPath IntermediaFolder => ProjectRoot.Combine("Intermedia");
	public NPath SourceFolder => ProjectRoot.Combine("Source");
	
	public Dictionary<string, ITargetInterface> TargetRules { get; } = new();
	public Dictionary<string, IModuleInterface> ModuleRules { get; } = new();

	private Dictionary<string, NPath> TargetRulePaths { get; } = new();
	private Dictionary<string, NPath> ModuleRulePaths { get; } = new();

	/// <summary>Packages materialized by the last <see cref="RestorePackages"/>, in dependency order.</summary>
	private List<RestoredPackage> RestoredPackages { get; } = new();

	/// <summary>
	/// Directories <see cref="ParseRules"/> globs for rule files on top of <c>Source/</c>: each
	/// restored package, plus the generated-rule directories synthesized for binary packages.
	/// </summary>
	private List<NPath> PackageRuleRoots { get; } = new();
	
	private IAssemblyCompileUnit BuildRuleCompileUnit { get; set; }
	private NPath CppBuildRuleProjectOutput => IntermediaFolder.Combine("CppBuildRule/Project");
	private NPath CppProjectOutput => IntermediaFolder.Combine("CppProject");
	private NPath CppBuildRuleBinaryOutput => IntermediaFolder.Combine("CppBuildRule/Binary");
	private NPath CppBuildRuleDllPath => CppBuildRuleBinaryOutput.Combine($"{BuildRuleCompileUnit.FileName}.dll");
	
	private List<BaseCppCompilePlugin> CppCompilePlugins { get; } = new();

}