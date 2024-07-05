using NiceIO;
using ReBuildTool.Service.Global;
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

	class Filter
	{
		public Guid FilterGuid;
		public string FilterName;
		public List<NPath> Files { get; } = new();

		public void Write(XmlCodeBuilder builder)
		{
			using (builder.CreateXmlScope(Tags.Filter, new Tuple<string, string>("Include", FilterName)))
			{
				builder.WriteNode("UniqueIdentifier", FilterGuid.ToString());
			}

			foreach (var path in Files)
			{
				var tag = Tags.None;
				if (IsHeader(path))
				{
					tag = Tags.ClInclude;
				}
				else if (IsSource(path))
				{
					tag = Tags.ClCompile;
				}
				using (builder.CreateXmlScope(tag,
					       new Tuple<string, string>("Include", path)))
				{
					builder.WriteNode(Tags.Filter, FilterName);
				}
			}
		}
		
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
				// GenerateRuleExtension();
				GenerateModules();
				FlushAllFilters();
			}
		}
	}

	private void GenerateTargets()
	{
		
		Filter filter = new Filter()
		{
			FilterGuid = Guid.NewGuid(),
			FilterName = InternalFilter.Targets
		};
		
		foreach (var targetFile in cppSource.SourceFolder.Files(true)
			         .Where(file => file.FileName.EndsWith(ICppProject.TargetDefineExtension)))
		{
			filter.Files.Add(targetFile.RelativeTo(outputFolder));
		}
		
		AllFilters.Add(filter.FilterName, filter);
	}

	private void GenerateModules()
	{
		Filter sourceFilter = new Filter()
		{
			FilterGuid = Guid.NewGuid(),
			FilterName = InternalFilter.Source
		};
		AllFilters.Add(sourceFilter.FilterName, sourceFilter);

		cppSource.ModuleRules.Values.ToList().ForEach(GenerateModule);
	}
	
	private void GenerateModule(IModuleInterface moduleInterface)
	{
		// generate all path filters
		var path = moduleInterface.ModuleDirectory.ToNPath();
		while (path.FileName != InternalFilter.Source)
		{
			if (!AllFilters.ContainsKey(path.ToString()))
			{
				var filter = new Filter()
				{
					FilterName = path.RelativeTo(cppSource.ProjectRoot),
					FilterGuid = Guid.NewGuid()
				};
				AllFilters.Add(path, filter);
			}

			path = path.Parent;
		}
		
		moduleInterface.ModuleDirectory.ToNPath().Files(true).ToList().ForEach(file =>
		{
			if (!AllFilters.TryGetValue(file.Parent, out var folderFilter))
			{
				folderFilter = new Filter()
				{
					FilterName = file.Parent.RelativeTo(cppSource.ProjectRoot),
					FilterGuid = Guid.NewGuid()
				};
				AllFilters.Add(file.Parent, folderFilter);
			}
			folderFilter.Files.Add(file.RelativeTo(outputFolder));
		});
	}

	private void FlushAllFilters()
	{
		foreach (var (key, filter) in AllFilters)
		{
			filter.Write(filterCodeBuilder);
		}
	}

	private Dictionary<string, Filter> AllFilters = new();

}