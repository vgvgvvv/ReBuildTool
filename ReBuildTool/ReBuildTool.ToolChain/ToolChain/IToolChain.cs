using NiceIO;

namespace ReBuildTool.ToolChain.ToolChain;


public abstract class IToolChain
{
	public BuildConfiguration Configuration { get; private set; }
	public Architecture Arch { get; private set; }
	
	protected IToolChain(BuildConfiguration configuration, Architecture arch)
	{
		Configuration = configuration;
		Arch = arch;
	}

	public abstract IEnumerable<NPath> ToolChainDefines();
	
	

}