﻿using NiceIO;
using ReBuildTool.Service.Global;
using ReBuildTool.Service.CompileService;
using ReBuildTool.Service.IDEService.VisualStudio;

namespace ReBuildTool.IDE.VisualStudio;

public class NetFrameworkCSProj : ISlnSubProject
{
	
	public class NetFrameworkCProjectConfiguration : IProjectConfiguration
	{
		private NetFrameworkCProjectConfiguration(string config, string platform)
		{
			ConfigurationName = config;
			PlatformName = platform;
		}

		public static NetFrameworkCProjectConfiguration Debug => new ("Debug", "AnyCPU");
		public static NetFrameworkCProjectConfiguration Release => new ("Release", "AnyCPU");

		public string ConfigurationName { get; }
		public string PlatformName { get; }
	}
	
	private static class Tags
	{
		public static string Project = nameof(Project);
		public static string PropertyGroup = nameof(PropertyGroup);
		public static string ItemGroup = nameof(ItemGroup);
		public static string Compile = nameof(Compile);
		public static string ProjectReference = nameof(ProjectReference);
	}
	
	public static NetFrameworkCSProj GenerateOrGetCSProj(SlnGenerator owner, IAssemblyCompileUnit unit, ICSharpCompileEnvironment env, NPath output)
	{
		if (owner.GetSubProj(unit.FileName, out var csProj))
		{
			if (csProj is NetFrameworkCSProj netProj)
			{
				return netProj;
			}

			throw new Exception("Project with same name already exists but is not a NetFrameworkCSProj");
		}

		var result = new NetFrameworkCSProj();
		result.name = unit.FileName;
		result.targetUnityAssembly = unit;
		result.icSharpCompileEnvironment = env;
		result.outputFolder = output;
		result.outputFolder.EnsureParentDirectoryExists();
		result.codeBuilder = new XmlCodeBuilder();
		result.ownerSln = owner;
		result.guid = Guid.NewGuid();
		result.GenerateProject();
		owner.RegisterProj(result);
		return result;
	}

	public List<IProjectConfiguration> projectConfigs { get; } = new()
	{
		NetFrameworkCProjectConfiguration.Debug,
		NetFrameworkCProjectConfiguration.Release
	};

	public void FlushToFile()
	{
		File.WriteAllText(outputFolder.Combine(targetUnityAssembly.FileName + ".csproj"), codeBuilder.ToString());
	}

	private void GenerateProject()
	{
		if (generated)
		{
			codeBuilder.Builder.Clear();
		}
		generated = true;
		codeBuilder.WriteHeader();
		using (codeBuilder.CreateXmlScope(Tags.Project,
			       new ("ToolsVersion", "4.0"),
			       new ("DefaultTargets", "Build"),
			       new ("xmlns", "http://schemas.microsoft.com/developer/msbuild/2003")))
		{
			GenerateHeader();
			GenerateConfigurations();
			GenerateOtherOptions();
			using (codeBuilder.CreateXmlScope("ItemGroup"))
			{
				GenerateSources();
				GenerateReferences();
			}

			using (codeBuilder.CreateXmlScope("ItemGroup"))
			{
				GenerateProjectReferences();
			}

			GenerateImport();
		}
	}

	private void GenerateHeader()
	{
		using (codeBuilder.CreateXmlScope(Tags.PropertyGroup))
		{
			codeBuilder.WriteNode("LangVersion", "latest");
			codeBuilder.WriteNode("DisableHandlePackageFileConflicts", "true");
		}
	}

	private void GenerateConfigurations()
	{
		using (codeBuilder.CreateXmlScope(Tags.PropertyGroup))
		{
			codeBuilder.WriteNode("Configuration", "Debug",
				new Tuple<string, string>("Condition", "$(Configuration)' == '' "));
			codeBuilder.WriteNode("Platform", "AnyCPU", new Tuple<string, string>("Condition", "$(Platform)' == '' "));
			codeBuilder.WriteNode("ProductVersion", "10.0.20506");
			codeBuilder.WriteNode("RootNamespace", "");
			codeBuilder.WriteNode("ProjectGuid", $"{{{guid}}}");
			codeBuilder.WriteNode("OutputType", targetUnityAssembly.CompileType.ToString());
			codeBuilder.WriteNode("AppDesignerFolder", "Properties");
			codeBuilder.WriteNode("AssemblyName", targetUnityAssembly.FileName);
			if (string.IsNullOrEmpty(targetUnityAssembly.TargetFrameworkVersion))
			{
				targetUnityAssembly.TargetFrameworkVersion = "4.7.1";
			}
			codeBuilder.WriteNode("TargetFrameworkVersion", targetUnityAssembly.TargetFrameworkVersion);
			codeBuilder.WriteNode("FileAlignment", "512");
			codeBuilder.WriteNode("BaseDirectory",icSharpCompileEnvironment.CsharpBuildRoot);
		}

		var allowUnsafe = icSharpCompileEnvironment.AllowUnsafe || targetUnityAssembly.Unsafe;
		using (codeBuilder.CreateXmlScope(Tags.PropertyGroup, 
			       new Tuple<string, string>("Condition", " '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' ")))
		{
			codeBuilder.WriteNode("DebugSymbols", "true");
			codeBuilder.WriteNode("DebugType", "full");
			codeBuilder.WriteNode("Optimize", "false");
			codeBuilder.WriteNode("OutputPath", outputFolder.Combine(@"bin\Debug"));
			var definitions = new List<string>();
			definitions.AddRange(targetUnityAssembly.Definitions);
			definitions.AddRange(icSharpCompileEnvironment.Definitions);
			definitions.Add("DEBUG");
			definitions.Add("TRACE");
			codeBuilder.WriteNode("DefineConstants", string.Join(';', definitions));
			codeBuilder.WriteNode("ErrorReport", "prompt");
			codeBuilder.WriteNode("WarningLevel", "4");
			codeBuilder.WriteNode("NoWarn", "0169");
			codeBuilder.WriteNode("AllowUnsafeBlocks", allowUnsafe.ToString());
			codeBuilder.WriteNode("TreatWarningsAsErrors", targetUnityAssembly.TreatWarningsAsErrors.ToString());
		}
		
		using (codeBuilder.CreateXmlScope(Tags.PropertyGroup, 
			       new Tuple<string, string>("Condition", " '$(Configuration)|$(Platform)' == 'Release|AnyCPU' ")))
		{
			codeBuilder.WriteNode("DebugType", "pdbonly");
			codeBuilder.WriteNode("Optimize", "true");
			codeBuilder.WriteNode("OutputPath", outputFolder.Combine(@"bin\Release"));
			var definitions = new List<string>();
			definitions.AddRange(targetUnityAssembly.Definitions);
			definitions.AddRange(icSharpCompileEnvironment.Definitions);
			codeBuilder.WriteNode("DefineConstants", string.Join(';', definitions));
			codeBuilder.WriteNode("ErrorReport", "prompt");
			codeBuilder.WriteNode("WarningLevel", "4");
			codeBuilder.WriteNode("NoWarn", "0169");
			codeBuilder.WriteNode("AllowUnsafeBlocks", allowUnsafe.ToString());
			codeBuilder.WriteNode("TreatWarningsAsErrors", targetUnityAssembly.TreatWarningsAsErrors.ToString());
		}
	}

	private void GenerateOtherOptions()
	{
		using (codeBuilder.CreateXmlScope(Tags.PropertyGroup))
		{
			codeBuilder.WriteNode("NoConfig", "true");
			codeBuilder.WriteNode("NoStdLib", "true");
			codeBuilder.WriteNode("AddAdditionalExplicitAssemblyReferences", "false");
			codeBuilder.WriteNode("ImplicitlyExpandNETStandardFacades", "false");
			codeBuilder.WriteNode("ImplicitlyExpandDesignTimeFacades", "false");
		}
	}
	
	private void GenerateSources()
	{
		foreach (var sourceFile in targetUnityAssembly.SourceFiles)
		{
			codeBuilder.WriteNodeWithoutValue(Tags.Compile, new Tuple<string, string>("Include", sourceFile.ToString()) );
		}
	}

	private void GenerateReferences()
	{
		foreach (var dll in targetUnityAssembly.ReferenceDlls)
		{
			using (codeBuilder.CreateXmlScope("Reference",
				       new Tuple<string, string>("Include", dll.FileNameWithoutExtension)))
			{
				codeBuilder.WriteNode("HintPath", dll);
			}
		}
	}

	private void GenerateProjectReferences()
	{
		foreach (var refUnit in targetUnityAssembly.References)
		{
			if (targetUnityAssembly == refUnit)
			{
				continue;
			}
			var csProj = GenerateOrGetCSProj(ownerSln, refUnit, icSharpCompileEnvironment, outputFolder);
			using (codeBuilder.CreateXmlScope(Tags.ProjectReference,
				       new Tuple<string, string>("Include", Path.Combine(csProj.outputFolder, csProj.name + ".csproj"))))
			{
				codeBuilder.WriteNode(Tags.Project, $"{{{csProj.guid}}}");
				codeBuilder.WriteNode("Name", csProj.name);
			}
		}
	}

	private void GenerateImport()
	{
		codeBuilder.WriteNodeWithoutValue("Import",
			new Tuple<string, string>("Project", @"$(MSBuildToolsPath)\Microsoft.CSharp.targets"));
	}

	public string name { get; private set; }
	public Guid guid { get; private set; }
	public NPath fullPath => outputFolder.Combine(name + ".csproj");
	public SlnGenerator ownerSln { get; private set; }
	private IAssemblyCompileUnit targetUnityAssembly;
	private ICSharpCompileEnvironment icSharpCompileEnvironment;
	private NPath outputFolder;
	private XmlCodeBuilder codeBuilder;
	private bool generated = false;
}