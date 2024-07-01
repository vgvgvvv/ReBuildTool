using ReBuildTool.Service.CompileService;

namespace ReBuildTool.IDE.VisualStudio;

public partial class VCProject
{
	private class InternalFilter
	{
		public static string Targets = nameof(Targets);
		public static string Source = nameof(Source);
		public static string RuleExtension = nameof(RuleExtension);
		public static string Modules = nameof(Modules);
	}
	
	private void GenerateFilter()
	{
		filterCodeBuilder.Builder.Clear();
		filterCodeBuilder.WriteHeader();
		using (filterCodeBuilder.CreateXmlScope(Tags.Project,
			       new Tuple<string, string>("ToolsVersion", "17.0"),
			       new Tuple<string, string>("xmlns", "http://schemas.microsoft.com/developer/msbuild/2003")))
		{
			using(filterCodeBuilder.CreateXmlScope("ItemGroup"))
			{
				GenerateTargets();

				GenerateRuleExtension();
				
				GenerateModules();

			}
		}
	}

	private void GenerateTargets()
	{
		using (filterCodeBuilder.CreateXmlScope(Tags.Filter, new Tuple<string, string>("Include", InternalFilter.Targets)))
		{
			filterCodeBuilder.WriteNode("UniqueIdentifier", $"{{{Guid.NewGuid()}}}");
		}
		
		foreach (var targetFile in cppSource.ProjectRoot.Files(true)
			         .Where(file => file.FileName.EndsWith(ICppProject.TargetDefineExtension)))
		{
			using (filterCodeBuilder.CreateXmlScope(Tags.None,
				       new Tuple<string, string>("Include", targetFile.RelativeTo(outputFolder))))
			{
				filterCodeBuilder.WriteNode(Tags.Filter, InternalFilter.Source);
			}
		}
	}

	private void GenerateRuleExtension()
	{
		using (filterCodeBuilder.CreateXmlScope(Tags.Filter, new Tuple<string, string>("Include", InternalFilter.RuleExtension)))
		{
			filterCodeBuilder.WriteNode("UniqueIdentifier", $"{{{Guid.NewGuid()}}}");
		}
		
		foreach (var targetFile in cppSource.ProjectRoot.Files(true)
			         .Where(file => file.FileName.EndsWith(ICppProject.ExtensionDefineExtension)))
		{
			using (filterCodeBuilder.CreateXmlScope(Tags.None,
				       new Tuple<string, string>("Include", targetFile.RelativeTo(outputFolder))))
			{
				filterCodeBuilder.WriteNode(Tags.Filter, InternalFilter.RuleExtension);
			}
		}
	}

	private void GenerateModules()
	{
		using (filterCodeBuilder.CreateXmlScope(Tags.Filter, new Tuple<string, string>("Include", InternalFilter.Source)))
		{
			filterCodeBuilder.WriteNode("UniqueIdentifier", $"{{{Guid.NewGuid()}}}");
		}
		
		using (filterCodeBuilder.CreateXmlScope(Tags.Filter, new Tuple<string, string>("Include", InternalFilter.Modules)))
		{
			filterCodeBuilder.WriteNode("UniqueIdentifier", $"{{{Guid.NewGuid()}}}");
		}
		
		foreach (var targetFile in cppSource.ProjectRoot.Files(true)
			         .Where(file => file.FileName.EndsWith(ICppProject.ModuleDefineExtension)))
		{
			using (filterCodeBuilder.CreateXmlScope(Tags.None,
				       new Tuple<string, string>("Include", targetFile.RelativeTo(outputFolder))))
			{
				filterCodeBuilder.WriteNode(Tags.Filter, InternalFilter.Modules);
			}
		}
		
		foreach (var (key, module) in cppSource.ModuleRules)
		{
			GenerateModule(module);
		}
	}
	
	private void GenerateModule(IModuleInterface moduleInterface)
	{
		
	}

}