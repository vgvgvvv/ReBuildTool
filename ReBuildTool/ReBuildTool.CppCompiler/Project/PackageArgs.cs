using ReBuildTool.Service.PackageService;
using ResetCore.Common;

namespace ReBuildTool.CppCompiler;

/// <summary>
/// Command line control over package restore.
///
/// This group lives in ReBuildTool.CppCompiler rather than next to the package service in
/// ReBuildTool.Service on purpose: <c>CmdParser</c> discovers argument groups by scanning
/// <c>AppDomain.CurrentDomain.GetAssemblies()</c>, and .NET loads assemblies lazily - a group in
/// an assembly nothing has touched yet would simply not be found. CppCompiler is already loaded
/// by the time arguments are parsed, as <c>CppCompilerArgs</c> itself proves.
/// </summary>
public class PackageArgs : CommandLineArgGroup<PackageArgs>
{
	[CmdLine("never access the network during package restore; fail if the lock is not already satisfied")]
	public CmdLineArg<bool> Offline { get; set; } = CmdLineArg<bool>.FromObject(nameof(Offline), false);

	[CmdLine("re-fetch every package even when the lock is already satisfied")]
	public CmdLineArg<bool> ForceRestore { get; set; } = CmdLineArg<bool>.FromObject(nameof(ForceRestore), false);

	[CmdLine("re-resolve moving pins (tags and branches) and rewrite RBTPackage.lock.json")]
	public CmdLineArg<bool> UpdateLock { get; set; } = CmdLineArg<bool>.FromObject(nameof(UpdateLock), false);

	[CmdLine("add a dependency to RBTPackage.json, e.g. MyLib=git:https://github.com/x/y.git#v1.0")]
	public CmdLineArg<string> PackageAdd { get; set; }

	[CmdLine("remove a dependency from RBTPackage.json by name")]
	public CmdLineArg<string> PackageRemove { get; set; }

	public PackageRestoreOptions ToRestoreOptions()
	{
		return new PackageRestoreOptions
		{
			Offline = Offline.Value,
			Force = ForceRestore.Value,
			UpdateLock = UpdateLock.Value
		};
	}
}
