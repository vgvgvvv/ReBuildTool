using NiceIO;
using ReBuildTool.Common;
using ReBuildTool.Service.CompileService;
using ReBuildTool.Service.IDEService.VisualStudio;

namespace ReBuildTool.IDE.VisualStudio;

public class VCProjectGenerator : ISlnSubProject
{
	private class Tags
	{
		public static string ItemGroup = nameof(ItemGroup);
		public static string ProjectConfiguration = nameof(ProjectConfiguration);
	}
	
	public string name { get; }
	public Guid guid { get; }
	public NPath fullPath { get; }

	public static VCProjectGenerator GenerateOrGetVCProj(SlnGenerator owner, ICppSourceProviderInterface unit, NPath output)
	{
		VCProjectGenerator result = new VCProjectGenerator();
		// TODO: generate vcproj file
		owner.RegisterProj(result);
		return result;
	}
	
	public void FlushToFile()
	{
		
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
	
	

	private XmlCodeBuilder codeBuilder = new XmlCodeBuilder();
}