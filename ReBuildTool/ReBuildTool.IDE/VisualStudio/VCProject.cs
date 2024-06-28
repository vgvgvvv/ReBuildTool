using NiceIO;
using ReBuildTool.Common;
using ReBuildTool.Service.CompileService;
using ReBuildTool.Service.IDEService.VisualStudio;

namespace ReBuildTool.IDE.VisualStudio;

public class VCProject : ISlnSubProject
{
	private class Tags
	{
		public static string ItemGroup = nameof(ItemGroup);
		public static string ProjectConfiguration = nameof(ProjectConfiguration);
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

	private void GenerateProject()
	{
		
	}

	private void GenerateFilter()
	{
		
	}

	private void GenerateCommonProps()
	{
		
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