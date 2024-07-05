using ResetCore.Common;

namespace ReBuildTool.Service.CommandGroup;

public interface ICommonCommandGroup : ICommandLineArgGroup
{
	public CmdLineArg<string> ProjectRoot { get; }
}