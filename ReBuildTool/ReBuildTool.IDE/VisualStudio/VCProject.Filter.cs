using NiceIO;
using ReBuildTool.Common;
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
		public string Path;
		public List<NPath> Files { get; } = new();

		public void Write(XmlCodeBuilder builder)
		{
			using (builder.CreateXmlScope(Tags.None, new Tuple<string, string>("Include", Path)))
			{
				builder.WriteNode("UniqueIdentifier", FilterGuid.ToString());
			}

			foreach (var path in Files)
			{
				using (builder.CreateXmlScope(Tags.None,
					       new Tuple<string, string>("Include", path)))
				{
					builder.WriteNode(Tags.Filter, Path);
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
				GenerateRuleExtension();
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
			Path = InternalFilter.Targets
		};
		
		foreach (var targetFile in cppSource.ProjectRoot.Files(true)
			         .Where(file => file.FileName.EndsWith(ICppProject.TargetDefineExtension)))
		{
			filter.Files.Add(targetFile.RelativeTo(outputFolder));
		}
		
		AllFilters.Add(filter.Path, filter);
	}

	private void GenerateRuleExtension()
	{
		Filter filter = new Filter()
		{
			FilterGuid = Guid.NewGuid(),
			Path = InternalFilter.RuleExtension
		};
		
		foreach (var targetFile in cppSource.ProjectRoot.Files(true)
			         .Where(file => file.FileName.EndsWith(ICppProject.ExtensionDefineExtension)))
		{
			filter.Files.Add(targetFile.RelativeTo(outputFolder));
		}
		
		AllFilters.Add(filter.Path, filter);
	}

	private void GenerateModules()
	{
		Filter sourceFilter = new Filter()
		{
			FilterGuid = Guid.NewGuid(),
			Path = InternalFilter.Source
		};
		AllFilters.Add(sourceFilter.Path, sourceFilter);
		
		Filter modulesFilter = new Filter()
		{
			FilterGuid = Guid.NewGuid(),
			Path = InternalFilter.Modules
		};
		
		foreach (var targetFile in cppSource.ProjectRoot.Files(true)
			         .Where(file => file.FileName.EndsWith(ICppProject.ModuleDefineExtension)))
		{
			modulesFilter.Files.Add(targetFile.RelativeTo(outputFolder));
		}
		
		AllFilters.Add(modulesFilter.Path, modulesFilter);

		cppSource.ModuleRules.Values.ToList().ForEach(GenerateModule);
	}
	
	private void GenerateModule(IModuleInterface moduleInterface)
	{
		// generate all path filters
		var path = new NPath(InternalFilter.Source).Combine(moduleInterface.ModuleDirectory.ToNPath().RelativeTo(cppSource.SourceFolder));
		while (path.ToString() != InternalFilter.Source)
		{
			if (!AllFilters.ContainsKey(path.ToString()))
			{
				var filter = new Filter()
				{
					Path = path,
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
					Path = file.Parent,
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