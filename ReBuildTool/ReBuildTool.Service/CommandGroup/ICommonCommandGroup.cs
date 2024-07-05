using NiceIO;
using ResetCore.Common;

namespace ReBuildTool.Service.CommandGroup;

public enum RunMode
{
	Init,
	Build,
	Clean,
	ReBuild
}

public interface ICommonCommandGroup : ICommandLineArgGroup
{
	public CmdLineArg<string> ProjectRoot { get; }
	
	public CmdLineArg<RunMode> Mode { get; }
	
}

public static class CommonCommandGroupExtension
{
	public static NPath GetProjectRoot(this ICommonCommandGroup group)
	{
		return group.ProjectRoot.Value.ToNPath();
	}
	
	public static NPath GetToolRoot(this ICommonCommandGroup group)
	{
		return typeof(CommonCommandGroupExtension).Assembly.Location.ToNPath().Parent.Parent.Parent.Parent;
	}

	public static NPath GetScriptRoot(this ICommonCommandGroup group)
	{
		return group.GetToolRoot().Combine("BuildScript");
	}

	public static NPath GetIntermediaPath(this ICommonCommandGroup group)
	{
		return group.GetProjectRoot().Combine("Intermedia");
	}
}