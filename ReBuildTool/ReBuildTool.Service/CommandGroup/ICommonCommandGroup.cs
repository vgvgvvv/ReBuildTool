using NiceIO;
using ResetCore.Common;

namespace ReBuildTool.Service.CommandGroup;

public enum RunMode
{
	Init,
	Build,
	Clean,
	ReBuild,

	/// <summary>
	/// Fetch the packages declared in RBTPackage.json and write the lock, without building.
	/// Every other mode restores implicitly, so this is for populating a checkout up front
	/// (a CI cache-warm step, or an offline machine's last online moment).
	/// </summary>
	Restore
}

public interface ICommonCommandGroup : ICommandLineArgGroup
{
	public CmdLineArg<string> ProjectRoot { get; }
	
	public CmdLineArg<RunMode> Mode { get; }
	
	public CmdLineArg<string> Target { get; }
	
}

public static class CommonCommandGroupExtension
{
	public static NPath GetProjectRoot(this ICommonCommandGroup group)
	{
		return group.ProjectRoot.Value.ToNPath();
	}
	
	public static NPath GetSourceRoot(this ICommonCommandGroup group)
	{
		return group.GetProjectRoot().Combine("Source");
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