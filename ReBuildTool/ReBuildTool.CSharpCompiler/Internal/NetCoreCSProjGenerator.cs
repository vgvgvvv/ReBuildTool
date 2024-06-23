using NiceIO;
using ReBuildTool.Common;

namespace ReBuildTool.CSharpCompiler;

internal class NetCoreCSProj : ISlnSubProject
{
	
	private static class Tags
	{
		public static string Project = nameof(Project);
		public static string PropertyGroup = nameof(PropertyGroup);
		public static string ItemGroup = nameof(ItemGroup);
		public static string Compile = nameof(Compile);
		public static string ProjectReference = nameof(ProjectReference);
	}
	
	public static NetCoreCSProj GenerateOrGetCSProj(SlnGenerator owner, IAssemblyCompileUnit unit,
		CompileEnvironment env, NPath output)
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
		result.compileEnvironment = env;
		result.outputFolder = output;
		result.outputFolder.EnsureParentDirectoryExists();
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
			codeBuilder.WriteNode("TargetFramework", targetUnityAssembly.TargetFrameworkVersion);
			codeBuilder.WriteNode("ImplicitUsings", "enable");
			codeBuilder.WriteNode("Nullable", "enable");
		}
	}
	
	private void GenerateConfigurations()
	{
		using (codeBuilder.CreateXmlScope(Tags.PropertyGroup, 
			       new Tuple<string, string>("Condition", " '$(Configuration)' == 'Debug' ")))
		{
			codeBuilder.WriteNode("AllowUnsafeBlocks", targetUnityAssembly.Unsafe ? "true" : "false");
		}
		
		using (codeBuilder.CreateXmlScope(Tags.PropertyGroup, 
			       new Tuple<string, string>("Condition", " '$(Configuration)' == 'Release' ")))
		{
			codeBuilder.WriteNode("AllowUnsafeBlocks", targetUnityAssembly.Unsafe ? "true" : "false");
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

		using (codeBuilder.CreateXmlScope(Tags.ItemGroup))
		{
			
		}
	}
	
	private void GenerateReferences()
	{
		
	}

	private void GenerateProjectReferences()
	{
		
	}
	
	public string name { get; private set; }
	public Guid guid { get; private set; }
	
	public SlnGenerator ownerSln { get; private set; }
	private IAssemblyCompileUnit targetUnityAssembly;
	private CompileEnvironment compileEnvironment;
	private NPath outputFolder;
	private XmlCodeBuilder codeBuilder;
	private bool generated = false;

	
}