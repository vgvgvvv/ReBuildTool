namespace ReBuildTool.IDE.VisualStudio;

public partial class VCProject
{
	private void GenerateCommonProps()
    {
	    filterCodeBuilder.Builder.Clear();
	    filterCodeBuilder.WriteHeader();
	    using (filterCodeBuilder.CreateXmlScope(Tags.Project,
		           new Tuple<string, string>("xmlns", "http://schemas.microsoft.com/developer/msbuild/2003")))
	    {
		    
	    }
    }
}