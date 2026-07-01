namespace ReBuildTool.IDE.VisualStudio;

public partial class VCProject
{
	private void GenerateCommonProps()
    {
	    // Microsoft.Cpp.Default.props is imported inline in the .vcxproj itself (before the
	    // per-config ConfigurationType blocks) - it must come before ConfigurationType is set,
	    // so it can't live in this shared file which is only imported after those blocks.
	    commonPropBuilder.Builder.Clear();
	    commonPropBuilder.WriteHeader();
	    using (commonPropBuilder.CreateXmlScope(Tags.Project,
		           new Tuple<string, string>("xmlns", "http://schemas.microsoft.com/developer/msbuild/2003")))
	    {
		    commonPropBuilder.WriteNodeWithoutValue(Tags.Import,
			    new Tuple<string, string>(Tags.Project, @"$(VCTargetsPath)\Microsoft.Cpp.props"));
	    }
    }
}