namespace ReBuildTool.ToolChain;

public interface IBuildConfigProvider
{
	IPlatformSupport GetBuildPlatform();

	BuildOptions GetBuildOptions();
	
}

internal class BuildConfigArgsProvider : IBuildConfigProvider
{
	public IPlatformSupport GetBuildPlatform()
	{
		// TODO: use current platform instead
		return new WindowsPlatformSupport();
	}

	public BuildOptions GetBuildOptions()
	{
		return new BuildOptions();
	}
}

public class CppBuilder
{

	public static IBuildConfigProvider DefaultBuildConfigProvider { get; } = new BuildConfigArgsProvider();
	
	public CppBuilder(IBuildConfigProvider? provider = null)
	{
		provider ??= DefaultBuildConfigProvider;
		
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