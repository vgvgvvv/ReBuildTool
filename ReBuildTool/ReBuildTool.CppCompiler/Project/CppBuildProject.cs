using System.Reflection;
using NiceIO;
using ReBuildTool.CppCompiler;
using ReBuildTool.Service.CompileService;
using ReBuildTool.Service.Context;
using ReBuildTool.Service.Global;
using ReBuildTool.Service.IDEService;
using ReBuildTool.Service.IDEService.CMake;
using ReBuildTool.Service.IDEService.VisualStudio;
using ResetCore.Common;

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

		if (targetFiles.Count == 0)
		{
			CreateDefaultProject();
			ParseRules();
			return;
		}
		
		foreach (var targetFile in targetFiles)
		{
			TargetRulePaths.Add(targetFile.FileNameWithoutExtension, targetFile);
		}
		
		foreach (var moduleFile in moduleFiles)
		{
			var fileName = moduleFile.FileName;
			ModuleRulePaths.Add(fileName.Substring(0, fileName.Length - ICppProject.ModuleDefineExtension.Length), moduleFile);
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

public class ${targetName}Target : TargetRule
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
			var defaultTargetContent = @"using ReBuildTool.ToolChain;

public class ${targetName}Module : ModuleRule
{
    public ${targetName}Module()
    {
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
		ParseRules();
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
		IntermediaFolder.DeleteIfExists(DeleteMode.Normal);
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
			if (dateTime < dllNewestTime)
			{
				Log.Warning("build tool has been updated, clean all");
				Clean();
			}
		}

		timeStampFile.EnsureParentDirectoryExists();
		timeStampFile.WriteAllText(dllNewestTime.ToLongTimeString());
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
				GenerateModuleCodes(rule);
				rule.ModuleDirectory = moduleRulePath.Parent;
				rule.SourceDirectories.Add(moduleRulePath.Parent.Combine("Public"));
				rule.SourceDirectories.Add(moduleRulePath.Parent.Combine("Private"));
				rule.PublicIncludePaths.Add(moduleRulePath.Parent.Combine("Public"));
				rule.PrivateIncludePaths.Add(moduleRulePath.Parent.Combine("Private"));
			}
			else
			{
				throw new Exception($"cannot find module {ruleName}");
			}
			ModuleRules.Add(ruleName, rule);
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
			builder.BuildTarget(targetRule);
		}
		else
		{
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
	
	private IAssemblyCompileUnit BuildRuleCompileUnit { get; set; }
	private NPath CppBuildRuleProjectOutput => IntermediaFolder.Combine("CppBuildRule/Project");
	private NPath CppProjectOutput => IntermediaFolder.Combine("CppProject");
	private NPath CppBuildRuleBinaryOutput => IntermediaFolder.Combine("CppBuildRule/Binary");
	private NPath CppBuildRuleDllPath => CppBuildRuleBinaryOutput.Combine($"{BuildRuleCompileUnit.FileName}.dll");
	
	private List<BaseCppCompilePlugin> CppCompilePlugins { get; } = new();

}