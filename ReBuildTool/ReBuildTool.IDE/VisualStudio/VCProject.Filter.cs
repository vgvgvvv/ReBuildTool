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
		GenerateUnassignedSourceFiles();
	}

	private void GenerateUnassignedSourceFiles()
	{
		var assignedFiles = AllFilters.Values
			.SelectMany(filter => filter.Files)
			.Select(path => path.ToString())
			.ToHashSet(StringComparer.OrdinalIgnoreCase);

		foreach (var file in cppSource.SourceFolder.Files(true))
		{
			var relativeFile = file.RelativeTo(outputFolder);
			if (!assignedFiles.Add(relativeFile.ToString()))
			{
				continue;
			}

			if (!AllFilters.TryGetValue(file.Parent, out var folderFilter))
			{
				folderFilter = new Filter
				{
					FilterName = file.Parent.RelativeTo(cppSource.ProjectRoot),
					FilterGuid = Guid.NewGuid()
				};
				AllFilters.Add(file.Parent, folderFilter);
			}

			folderFilter.Files.Add(relativeFile);
		}
	}
	
	private void GenerateModule(IModuleInterface moduleInterface)
	{
		var moduleDirectory = moduleInterface.ModuleDirectory.ToNPath();

		// Walk up from the module, one filter per ancestor, stopping at the project's Source
		// folder. Only a module that lives under Source/ ever reaches that sentinel: a package's
		// module sits under Packages/, and a package pulled in by path is not under the project at
		// all. So the walk has to stop at the project root and at the filesystem root too -
		// without those guards it runs off the top of the tree and NPath.FileName throws
		// ("not valid on a root level directory").
		var path = moduleDirectory;
		while (!path.IsRoot
		       && path.FileName != InternalFilter.Source
		       && path != cppSource.ProjectRoot
		       && path.IsChildOf(cppSource.ProjectRoot))
		{
			GetOrAddFilter(path, moduleDirectory);
			path = path.Parent;
		}

		moduleDirectory.Files(true).ToList().ForEach(file =>
		{
			GetOrAddFilter(file.Parent, moduleDirectory).Files.Add(file.RelativeTo(outputFolder));
		});
	}

	private Filter GetOrAddFilter(NPath directory, NPath moduleDirectory)
	{
		if (AllFilters.TryGetValue(directory, out var existing))
		{
			return existing;
		}

		var filter = new Filter
		{
			FilterName = FilterNameFor(directory, moduleDirectory),
			FilterGuid = Guid.NewGuid()
		};
		AllFilters.Add(directory, filter);
		return filter;
	}

	/// <summary>
	/// The virtual folder a file appears under in Solution Explorer. It has to be a downward
	/// path: anything inside the project is named relative to it, but a package consumed through
	/// a path dependency lives outside the project entirely, and naming that relative to the
	/// project root would yield a "..\..\" filter. Those are grouped under
	/// <see cref="InternalFilter.Modules"/> by the package directory's own name instead.
	/// </summary>
	private string FilterNameFor(NPath directory, NPath moduleDirectory)
	{
		if (directory.IsChildOf(cppSource.ProjectRoot))
		{
			return directory.RelativeTo(cppSource.ProjectRoot).ToString();
		}

		var moduleRoot = InternalFilter.Modules.ToNPath().Combine(moduleDirectory.FileName);
		return directory == moduleDirectory
			? moduleRoot.ToString()
			: moduleRoot.Combine(directory.RelativeTo(moduleDirectory)).ToString();
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
