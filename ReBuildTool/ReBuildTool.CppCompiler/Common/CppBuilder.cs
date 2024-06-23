namespace ReBuildTool.ToolChain;

public interface IBuildConfigProvider
{
	IPlatformSupport GetBuildPlatform();

	BuildOptions GetBuildOptions();
	
}

public class CppBuilder
{
	public CppBuilder(IBuildConfigProvider provider)
	{
		CurrentPlatformSupport = provider.GetBuildPlatform();
		CurrentBuildOption = provider.GetBuildOptions();
		var configuration = CurrentBuildOption.Configuration;
		var arch = CurrentBuildOption.Architecture;
		CurrentToolChain = CurrentPlatformSupport.MakeCppToolChain(arch, configuration);
	}
	
	public void BuildTarget(TargetRule targetRule)
	{
		
	}

	public IToolChain CurrentToolChain { get; }
	
	public BuildOptions CurrentBuildOption { get; }
	
	public IPlatformSupport CurrentPlatformSupport { get; } 
}