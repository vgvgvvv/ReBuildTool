using NiceIO;
using ReBuildTool.Common;
using ReBuildTool.IDE.VisualStudio;
using ReBuildTool.Service.CompileService;
using ReBuildTool.Service.IDEService.VisualStudio;

namespace ReBuildTool.IDE.VisualStudio;

public class NetCoreCSProj : ISlnSubProject
{
	
	private static class Tags
	{
		public static string Project = nameof(Project);
		public static string PropertyGroup = nameof(PropertyGroup);
		public static string ItemGroup = nameof(ItemGroup);
		public static string Compile = nameof(Compile);
		public static string Reference = nameof(Reference);
		public static string ProjectReference = nameof(ProjectReference);
	}
	
	public static NetCoreCSProj GenerateOrGetCSProj(SlnGenerator owner, IAssemblyCompileUnit unit,
		ICSharpCompileEnvironment env, NPath output)
	{
		if (owner.GetSubProj(unit.FileName, out var csProj))
		{
			if (csProj is NetCoreCSProj netProj)
			{
				return netProj;
			}

			throw new Exception("Project with same name already exists but is not a NetCoreCSProj");
		}
		
		var result = new NetCoreCSProj();
		result.name = unit.FileName;
		result.targetUnityAssembly = unit;
		result.icSharpCompileEnvironment = env;
		result.outputFolder = output;
		result.outputFolder.EnsureDirectoryExists();
		result.codeBuilder = new XmlCodeBuilder();
		result.ownerSln = owner;
		result.guid = Guid.NewGuid();
		result.GenerateProject();
		owner.RegisterCsProj(result);
		return result;
	}


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

		using (codeBuilder.CreateXmlScope(Tags.Project,
			       new Tuple<string, string>("Sdk", "Microsoft.NET.Sdk")))
		{
			GenerateHeader();
			GenerateConfigurations();
			GenerateOtherOptions();

			GenerateSources();
			GenerateReferences();
			GenerateProjectReferences();
		}
	}

	private void GenerateHeader()
	{
		using (codeBuilder.CreateXmlScope(Tags.PropertyGroup))
		{
			if (string.IsNullOrEmpty(targetUnityAssembly.TargetFrameworkVersion))
			{
				targetUnityAssembly.TargetFrameworkVersion = "net8.0";
			}
			codeBuilder.WriteNode("TargetFramework", targetUnityAssembly.TargetFrameworkVersion);
			codeBuilder.WriteNode("ImplicitUsings", "enable");
			codeBuilder.WriteNode("Nullable", "enable");
			switch (targetUnityAssembly.CompileType)
			{
				case CompileOutputType.Library:
					codeBuilder.WriteNode("OutputType", "Library");
					break;
				case CompileOutputType.Exe:
					codeBuilder.WriteNode("OutputType", "Exe");
					break;
			}

			if (!string.IsNullOrEmpty(targetUnityAssembly.RootNamespace))
			{
				codeBuilder.WriteNode("RootNamespace", targetUnityAssembly.RootNamespace);
			}
		}
	}
	
	private void GenerateConfigurations()
	{
		using (codeBuilder.CreateXmlScope(Tags.PropertyGroup, 
			       new Tuple<string, string>("Condition", " '$(Configuration)' == 'Debug' ")))
		{
			codeBuilder.WriteNode("AllowUnsafeBlocks", targetUnityAssembly.Unsafe ? "true" : "false");
			codeBuilder.WriteNode("NoWarn", "1701;1702;");
			codeBuilder.WriteNode("WarningLevel", "4");
			codeBuilder.WriteNode("TreatWarningsAsErrors", targetUnityAssembly.TreatWarningsAsErrors.ToString());
			var definitions = new List<string>();
			definitions.Add("TRACE");
			definitions.Add("DEBUG");
			definitions.AddRange(targetUnityAssembly.Definitions);
			definitions.AddRange(icSharpCompileEnvironment.Definitions);
			codeBuilder.WriteNode("DefineConstants", string.Join(';', definitions));
			codeBuilder.WriteNode("OutputPath", outputFolder.Combine(@"bin\Debug"));
		}
		
		using (codeBuilder.CreateXmlScope(Tags.PropertyGroup, 
			       new Tuple<string, string>("Condition", " '$(Configuration)' == 'Release' ")))
		{
			codeBuilder.WriteNode("AllowUnsafeBlocks", targetUnityAssembly.Unsafe ? "true" : "false");
			codeBuilder.WriteNode("NoWarn", "1701;1702;");
			codeBuilder.WriteNode("WarningLevel", "4");
			codeBuilder.WriteNode("TreatWarningsAsErrors", targetUnityAssembly.TreatWarningsAsErrors.ToString());
			var definitions = new List<string>();
			definitions.AddRange(targetUnityAssembly.Definitions);
			definitions.AddRange(icSharpCompileEnvironment.Definitions);
			codeBuilder.WriteNode("DefineConstants", string.Join(';', definitions));
			codeBuilder.WriteNode("OutputPath", outputFolder.Combine(@"bin\Release"));
		}
	}
	
	private void GenerateOtherOptions()
	{
		
	}

	private void GenerateSources()
	{
		using (codeBuilder.CreateXmlScope(Tags.ItemGroup))
		{
			codeBuilder.WriteNodeWithoutValue("Compile", 
				new Tuple<string, string>("Remove", "*"));
		}

		var rootPath = Path.GetDirectoryName(ownerSln.OutputPath);
		using (codeBuilder.CreateXmlScope(Tags.ItemGroup))
		{
			foreach (var sourceFile in targetUnityAssembly.SourceFiles)
			{
				var relativeToRoot = sourceFile.RelativeTo(rootPath.ToNPath());
				var relativeToProject = sourceFile.RelativeTo(outputFolder);
				using (codeBuilder.CreateXmlScope("Compile",
					       new Tuple<string, string>("Include", relativeToProject)))
				{
					codeBuilder.WriteNode("Link", relativeToRoot);
				}
			}
		}
	}
	
	private void GenerateReferences()
	{
		if (targetUnityAssembly.ReferenceDlls.Count == 0)
		{
			return;	
		}
		using (codeBuilder.CreateXmlScope(Tags.ItemGroup))
		{
			foreach (var referenceDll in targetUnityAssembly.ReferenceDlls)
			{
				using (codeBuilder.CreateXmlScope(
					       Tags.Reference, 
					       new Tuple<string, string>("Include", referenceDll.FileNameWithoutExtension)))
				{
					codeBuilder.WriteNode("HintPath", referenceDll);
				}
			}
			
		}
	}

	private void GenerateProjectReferences()
	{
		if (targetUnityAssembly.References.Count == 0)
		{
			return;	
		}

		using (codeBuilder.CreateXmlScope(Tags.ItemGroup))
		{
			foreach (var compileUnit in targetUnityAssembly.References)
			{
				var proj = GenerateOrGetCSProj(ownerSln, compileUnit, icSharpCompileEnvironment, outputFolder);
				
				codeBuilder.WriteNodeWithoutValue("ProjectReference", 
					new Tuple<string, string>("Include", proj.outputFolder.Combine(proj.name + ".csproj")));
			}
		}
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