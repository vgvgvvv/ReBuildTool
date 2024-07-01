using NiceIO;
using ReBuildTool.Common;
using ReBuildTool.Service.CompileService;
using ReBuildTool.Service.IDEService.VisualStudio;

namespace ReBuildTool.IDE.VisualStudio;

public partial class VCProject : ISlnSubProject
{
	private class Tags
	{
		public static string ProjectConfiguration = nameof(ProjectConfiguration);
		public static string Project = nameof(Project);
		public static string ItemGroup = nameof(ItemGroup);
		public static string PropertyGroup = nameof(PropertyGroup);
		public static string Filter = nameof(Filter);
		public static string ClCompile = nameof(ClCompile);
		public static string ClInclude = nameof(ClInclude);
		public static string None = nameof(None);
		public static string Import = nameof(Import);
	}
	
	

	public static VCProject GenerateOrGetVCProj(SlnGenerator owner, ICppSourceProviderInterface source, NPath output)
	{
		if (owner.GetSubProj(source.Name, out var subProject))
		{
			if (subProject is VCProject vcProj)
			{
				return vcProj;
			}

			throw new Exception("Project with same name already exists but is not a NetCoreCSProj");
		}

		VCProject result = new VCProject();
		result.name = source.Name;
		result.guid = Guid.NewGuid();
		result.outputFolder = output;
		result.cppSource = source;
		result.outputFolder.EnsureDirectoryExists();
		result.projectCodeBuilder = new XmlCodeBuilder();
		result.filterCodeBuilder = new XmlCodeBuilder();
		result.commonPropBuilder = new XmlCodeBuilder();
		result.ownerSln = owner;
		result.GenerateProject();
		result.GenerateFilter();
		result.GenerateCommonProps();
		owner.RegisterProj(result);
		return result;
	}
	
	public void FlushToFile()
	{
		File.WriteAllText(fullPath, projectCodeBuilder.ToString());
		File.WriteAllText(filterPath, filterCodeBuilder.ToString());
		File.WriteAllText(commonPropPath, commonPropBuilder.ToString());
	}

	private bool IsHeader(NPath path)
	{
		return path.ExtensionWithDot == ".h" || 
		       path.ExtensionWithDot == "hpp" || 
		       path.ExtensionWithDot == "inl";
	}
		
	private bool IsSource(NPath path)
	{
		return path.ExtensionWithDot == ".cpp" || 
		       path.ExtensionWithDot == "c" ||
		       path.ExtensionWithDot == "cc" ||
		       path.ExtensionWithDot == "cxx" ||
		       path.ExtensionWithDot == ".asm";
	}

	private bool IsOther(NPath path)
	{
		return !IsHeader(path) && !IsSource(path);
	}
	
	public string name { get; private set; }
    public Guid guid { get; private set; }
    public NPath fullPath => outputFolder.Combine(name + ".vcxproj");
    public NPath filterPath => outputFolder.Combine(name + ".vcxproj.filter");
    public NPath commonPropPath => outputFolder.Combine(name + ".prop");
	
	public SlnGenerator ownerSln { get; private set; }
	private ICppSourceProviderInterface cppSource;
	private NPath outputFolder;
	private XmlCodeBuilder projectCodeBuilder;
	private XmlCodeBuilder filterCodeBuilder;
	private XmlCodeBuilder commonPropBuilder;
}