namespace ReBuildTool.IDE.VisualStudio;

public partial class VCProject
{
	private void GenerateCommonProps()
    {
	    commonPropBuilder.Builder.Clear();
	    commonPropBuilder.WriteHeader();
	    using (commonPropBuilder.CreateXmlScope(Tags.Project,
		           new Tuple<string, string>("xmlns", "http://schemas.microsoft.com/developer/msbuild/2003")))
	    {
		    
	    }
    }
}