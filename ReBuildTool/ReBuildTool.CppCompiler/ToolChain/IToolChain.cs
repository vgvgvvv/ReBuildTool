using System.Diagnostics;
using NiceIO;
using ReBuildTool.Service.Global;
using ResetCore.Common;

namespace ReBuildTool.ToolChain;

public enum CppVersion
{
	Cpp11,
	Cpp14,
	Cpp17,
	Cpp20,
	Latest
}

/// <summary>
/// Cross-toolchain warning level. Each toolchain maps this to its native
/// warning flags (MSVC /W0..4, GCC/Clang -w/-Wall/-Wextra/-Wpedantic).
/// <c>Default</c> matches the previous hard-coded behaviour (/W3 on MSVC,
/// no flag on GCC/Clang).
/// </summary>
public enum WarningLevel
{
	/// <summary>Disable all warnings: MSVC /W0, GCC/Clang -w.</summary>
	None,
	/// <summary>MSVC /W1, GCC/Clang -w (no direct equivalent; minimal).</summary>
	Minimal,
	/// <summary>MSVC /W3 (the historic default), GCC/Clang default (no flag).</summary>
	Default,
	/// <summary>MSVC /W4 / GCC/Clang -Wall.</summary>
	All,
	/// <summary>MSVC /W4 / GCC/Clang -Wall -Wextra.</summary>
	Extra,
	/// <summary>MSVC /Wall / GCC/Clang -Wall -Wextra -Wpedantic.</summary>
	Pedantic,
}

/// <summary>
/// Cross-toolchain optimization level. Maps to MSVC /Od /O1 /O2 /Ox and
/// GCC/Clang -O0 -Os -O2 -O3. <c>null</c> (the unset state) means follow the
/// <see cref="BuildConfiguration"/> (Debug=-O0, Release=-O3, ReleaseSize=-Os/size).
/// </summary>
public enum OptimizationLevel
{
	/// <summary>Disable optimization: MSVC /Od, GCC/Clang -O0.</summary>
	None,
	/// <summary>Optimize for size: MSVC /O1, GCC/Clang -Oz (clang) / -Os (gcc).</summary>
	Size,
	/// <summary>Standard speed optimization: MSVC /O2, GCC/Clang -O2.</summary>
	Speed,
	/// <summary>Maximum speed optimization: MSVC /Ox, GCC/Clang -O3.</summary>
	MaxSpeed,
}

/// <summary>
/// C runtime library to link against (MSVC only — /MT /MTd /MD /MDd).
/// GCC/Clang ignore this (they select CRT implicitly via the toolchain/sysroot).
/// <c>null</c> means use the toolchain default (/MTd Debug, /MT Release).
/// </summary>
public enum CRunTimeType
{
	/// <summary>Static debug CRT: /MTd.</summary>
	DebugStatic,
	/// <summary>Static release CRT: /MT.</summary>
	ReleaseStatic,
	/// <summary>Dynamic debug CRT: /MDd.</summary>
	DebugDynamic,
	/// <summary>Dynamic release CRT: /MD.</summary>
	ReleaseDynamic,
}

/// <summary>
/// Linker subsystem (mainly the Windows/MSVC /SUBSYSTEM: switch, also honored
/// by MinGW). Native Linux/macOS linkers have no subsystem concept and ignore it.
/// </summary>
public enum Subsystem
{
	/// <summary>Console subsystem: /SUBSYSTEM:CONSOLE.</summary>
	Console,
	/// <summary>Windowing subsystem (no console): /SUBSYSTEM:WINDOWS.</summary>
	Windows,
}


public abstract class IToolChain
{
	public abstract string Name { get; }
	
	public BuildConfiguration Configuration { get; private set; }
	public Architecture Arch { get; private set; }
	
	protected IToolChain(BuildConfiguration configuration, Architecture arch)
	{
		Configuration = configuration;
		Arch = arch;
	}
	
	public abstract IEnumerable<string> ToolChainDefines();

	public abstract Dictionary<string, string> EnvVars();

	public abstract IEnumerable<NPath> ToolChainIncludePaths();

	public abstract IEnumerable<NPath> ToolChainLibraryPaths();

	public abstract IEnumerable<string> ToolChainStaticLibraries();
	
	public abstract IEnumerable<string> ToolChainDynamicLibraries();

	public abstract bool CanBeCompiled(NPath sourceFile);

	public abstract string ObjectExtension { get; }
	
	public abstract string ExecutableExtension { get; }
	
	public abstract string StaticLibraryExtension { get; }
	
	public abstract string DynamicLibraryExtension { get; }
	
	public virtual string LibraryPrefix => string.Empty;

	public abstract NPath CompilerExecutableFor(NPath sourceFile);
	
	public abstract IEnumerable<string> CompileArgsFor(CppCompilationUnit cppCompilationInstruction);
	
	internal abstract CppCompileInvocation MakeCompileInvocation(CppCompilationUnit compileUnit);
	
	internal abstract CppLinkInvocation MakeLinkInvocation(CppLinkUnit cppLinkUnit);
	
	internal abstract CppArchiveInvocation MakeArchiveInvocation(CppArchiveUnit cppArchiveUnit);
	
	public abstract ICompileArgsBuilder MakeCompileArgsBuilder();
	
	public abstract ILinkArgsBuilder MakeLinkArgsBuilder();
	
	public abstract IArchiveArgsBuilder MakeArchiveArgsBuilder();
	
}

internal class InvocationBase
{
	public bool Run()
	{
		using (var shell = Shell.Create())
		{
			shell.WithProgram(ProgramName)
				.WithArguments(Arguments)
				.WithEnvVars(EnvVars)
				.Execute()
				.WaitForEnd();

			return shell.IsSuccess();
		}
		
	}
		
	public string ProgramName { get; set; }
	public  List<string> Arguments { get; } = new();
	public  Dictionary<string, string> EnvVars { get; } = new();

	public override string ToString()
	{
		return $"\"{ProgramName}\" {string.Join(' ', Arguments)}";
	}
}

internal class CppCompileInvocation : InvocationBase
{
	public CppCompileInvocation(CppCompilationUnit unit)
	{
		Unit = unit;
	}
	public CppCompilationUnit Unit { get; }
}

internal class CppLinkInvocation : InvocationBase
{
	public CppLinkInvocation(CppLinkUnit unit)
	{
		Unit = unit;
	}
	public CppLinkUnit Unit { get; }
}

internal class CppArchiveInvocation : InvocationBase
{
	public CppArchiveInvocation(CppArchiveUnit unit)
	{
		Unit = unit;
	}
	public CppArchiveUnit Unit { get; }
}

public class IArgsBuilder
{
	public void Append(string arg)
	{
		Arguments.Add(arg);
	}
	
	public virtual IEnumerable<string> GetAllArguments()
	{
		return Arguments;
	}
	
	private List<string> Arguments { get; } = new();
}

public abstract class ICompileArgsBuilder : IArgsBuilder
{
	public abstract void DisableException(bool enable);

	public abstract void DisableWarnings(string warnCode);
	
	public abstract void SetWarnAsError(bool enable);
	
	public abstract void SetLto(bool enable);

	public CppVersion CppStandard { get; private set; } = CppVersion.Latest;

	/// <summary>
	/// Override the C++ language standard for this toolchain (e.g. to Cpp20
	/// or Latest). Modules call this via AdditionCompileArgs(builder) when they
	/// need a newer standard than the default. Default here is Latest because
	/// most modern engines target C++20/23.
	/// </summary>
	public void SetCppStandard(CppVersion version)
	{
		CppStandard = version;
	}
	
	public abstract string CppStandardFlag { get; }
	
	/// <summary>
	/// Whether RTTI (Run-Time Type Information) is enabled for this compile
	/// unit. Disabled by default for smaller/faster binaries, but engines that
	/// rely on <c>dynamic_cast</c> or reflection enable it via
	/// <see cref="SetEnableRTTI"/> from <c>AdditionCompileArgs</c>.
	/// </summary>
	public bool EnableRTTI { get; private set; } = false;

	public abstract string RTTIFlag { get; }

	/// <summary>
	/// Enable or disable RTTI for this compile unit. Modules call this from
	/// <see cref="CppModuleRule.AdditionCompileArgs"/> when their code needs
	/// <c>dynamic_cast</c> or typeid support.
	/// </summary>
	public void SetEnableRTTI(bool enable)
	{
		EnableRTTI = enable;
	}

	/// <summary>
	/// Whether C++ exception handling is enabled for this compile unit.
	/// Disabled by default. Enable via <see cref="SetEnableException"/> from
	/// <c>AdditionCompileArgs</c> when the module needs throw/try/catch.
	/// </summary>
	public bool EnableException { get; private set; } = false;

	public abstract IEnumerable<string> ExceptionFlags { get; }

	/// <summary>
	/// Enable or disable C++ exception handling for this compile unit.
	/// Modules call this from <see cref="CppModuleRule.AdditionCompileArgs"/>.
	/// </summary>
	public void SetEnableException(bool enable)
	{
		EnableException = enable;
	}

	/// <summary>
	/// Warning level override. <c>null</c> (unset) means use the toolchain's
	/// hard-coded default (/W3 on MSVC, no flag on GCC/Clang). Setting this
	/// suppresses the default so the two don't conflict.
	/// </summary>
	public WarningLevel? WarningLevel { get; private set; }

	/// <summary>
	/// Native warning flags derived from <see cref="WarningLevel"/>; empty
	/// when <see cref="WarningLevel"/> is null.
	/// </summary>
	public abstract IEnumerable<string> WarningFlags { get; }

	/// <summary>
	/// Override the warning level for this compile unit. Modules call this
	/// from <see cref="CppModuleRule.AdditionCompileArgs"/> to raise/lower
	/// warnings per module (e.g. to /W4 or -Wall -Wextra).
	/// </summary>
	public void SetWarningLevel(WarningLevel level)
	{
		WarningLevel = level;
	}

	/// <summary>
	/// Optimization level override. <c>null</c> (unset) means follow the
	/// <see cref="BuildConfiguration"/> (the previous behaviour). Setting this
	/// suppresses the config-driven -O flag.
	/// </summary>
	public OptimizationLevel? OptimizationLevel { get; private set; }

	/// <summary>
	/// Native optimization flag derived from <see cref="OptimizationLevel"/>;
	/// empty when <see cref="OptimizationLevel"/> is null.
	/// </summary>
	public abstract IEnumerable<string> OptimizationFlags { get; }

	/// <summary>
	/// Override the optimization level for this compile unit (e.g. force -O2
	/// in Debug). Modules call this from
	/// <see cref="CppModuleRule.AdditionCompileArgs"/>.
	/// </summary>
	public void SetOptimizationLevel(OptimizationLevel level)
	{
		OptimizationLevel = level;
	}

	/// <summary>
	/// C runtime library override (MSVC only). <c>null</c> means use the
	/// toolchain default (/MTd Debug, /MT Release).
	/// </summary>
	public CRunTimeType? CRunTimeType { get; private set; }

	/// <summary>
	/// Native CRT flag(s) derived from <see cref="CRunTimeType"/>; empty on
	/// GCC/Clang (which don't model CRT selection this way).
	/// </summary>
	public abstract IEnumerable<string> CRunTimeFlags { get; }

	/// <summary>
	/// Override the C runtime library (MSVC only: /MT /MTd /MD /MDd). Modules
	/// call this from <see cref="CppModuleRule.AdditionCompileArgs"/>.
	/// </summary>
	public void SetCRunTimeType(CRunTimeType crt)
	{
		CRunTimeType = crt;
	}

	/// <summary>
	/// Position-Independent Code override. <c>null</c> means use the
	/// toolchain default (Android always -fPIC, others omit). Setting this
	/// forces -fPIC / -fno-pic (GCC/Clang only; MSVC ignores — PE doesn't use PIC).
	/// </summary>
	public bool? EnablePIC { get; private set; }

	/// <summary>
	/// Native PIC flag derived from <see cref="EnablePIC"/>; empty when null
	/// or on MSVC.
	/// </summary>
	public abstract IEnumerable<string> PicFlags { get; }

	/// <summary>
	/// Force enable/disable position-independent code (GCC/Clang only). Useful
	/// for building objects destined for a shared library on Linux/macOS.
	/// </summary>
	public void SetEnablePIC(bool enable)
	{
		EnablePIC = enable;
	}

	/// <summary>
	/// Serialize all configured arguments to native flags.
	/// </summary>
	/// <param name="isCSource">Must be true for plain C (.c) translation units:
	/// they are compiled with a C compiler/driver mode and must not receive the
	/// C++-only std/RTTI/exception flags (gcc rejects them outright; clang/MSVC
	/// warn). Warning/optimization/CRT/PIC flags below are language-agnostic and
	/// are emitted for both C and C++.</param>
	public IEnumerable<string> GetAllArguments(bool isCSource)
	{
		foreach (var argument in base.GetAllArguments())
		{
			yield return argument;
		}

		// Language-agnostic flags — apply to both C and C++ sources.
		foreach (var warnFlag in WarningFlags)
		{
			yield return warnFlag;
		}
		foreach (var optFlag in OptimizationFlags)
		{
			yield return optFlag;
		}
		foreach (var crtFlag in CRunTimeFlags)
		{
			yield return crtFlag;
		}
		foreach (var picFlag in PicFlags)
		{
			yield return picFlag;
		}

		if (isCSource)
		{
			yield break;
		}

		yield return CppStandardFlag;
		yield return RTTIFlag;
		foreach (var exceptionFlag in ExceptionFlags)
		{
			yield return exceptionFlag;
		}
	}
}

public abstract class ILinkArgsBuilder : IArgsBuilder
{
	public abstract void DisableWarnings(string warnCode);

	public abstract void SetLto(bool enable);

	public abstract void SetFastLink(bool enable);

	public abstract void SetWarnAsError(bool enable);

	/// <summary>
	/// Subsystem override (mainly MSVC /SUBSYSTEM:). <c>null</c> means let the
	/// linker pick its default. Windows GUI apps set this to
	/// <see cref="Subsystem.Windows"/> to avoid a console window.
	/// </summary>
	public Subsystem? Subsystem { get; private set; }

	/// <summary>
	/// Native subsystem flag(s); empty on linkers without a subsystem concept.
	/// </summary>
	public abstract IEnumerable<string> SubsystemFlags { get; }

	/// <summary>
	/// Override the linker subsystem. Modules call this from
	/// <see cref="CppModuleRule.AdditionLinkArgs"/>.
	/// </summary>
	public void SetSubsystem(Subsystem subsystem)
	{
		Subsystem = subsystem;
	}

	/// <summary>
	/// Module-definition file (.def) path for the linker to control exports
	/// (MSVC /DEF:, GCC/Clang -Wl,--version-script). <c>null</c> = none.
	/// </summary>
	public string? ModuleDefinitionFile { get; private set; }

	/// <summary>
	/// Native module-definition flag(s); empty when no .def is set.
	/// </summary>
	public abstract IEnumerable<string> ModuleDefinitionFlags { get; }

	/// <summary>
	/// Specify a module-definition (.def) file for the link step. Modules call
	/// this from <see cref="CppModuleRule.AdditionLinkArgs"/>.
	/// </summary>
	public void SetModuleDefinitionFile(string path)
	{
		ModuleDefinitionFile = path;
	}

	/// <summary>
	/// Manifest generation override (MSVC only). <c>null</c> means use the
	/// linker default (manifest on). Set false to pass /MANIFEST:NO.
	/// </summary>
	public bool? GenerateManifest { get; private set; }

	/// <summary>
	/// Native manifest flag(s); empty on non-MSVC linkers.
	/// </summary>
	public abstract IEnumerable<string> ManifestFlags { get; }

	/// <summary>
	/// Enable/disable manifest generation (MSVC only). Modules call this from
	/// <see cref="CppModuleRule.AdditionLinkArgs"/>.
	/// </summary>
	public void SetGenerateManifest(bool enable)
	{
		GenerateManifest = enable;
	}

	/// <summary>
	/// Incremental linking override (MSVC only). <c>null</c> means use the
	/// toolchain default (currently /INCREMENTAL:NO). Set true to enable.
	/// </summary>
	public bool? EnableIncrementalLink { get; private set; }

	/// <summary>
	/// Native incremental-link flag(s); empty when unset or on non-MSVC linkers.
	/// </summary>
	public abstract IEnumerable<string> IncrementalLinkFlags { get; }

	/// <summary>
	/// Enable/disable incremental linking (MSVC only). Modules call this from
	/// <see cref="CppModuleRule.AdditionLinkArgs"/>.
	/// </summary>
	public void SetEnableIncrementalLink(bool enable)
	{
		EnableIncrementalLink = enable;
	}

	/// <summary>
	/// Dead-code elimination override (removes unreferenced functions/data
	/// from the final binary). <c>null</c> means use the toolchain default
	/// (MSVC: /OPT:REF on non-Debug; GCC/Clang: -Wl,--gc-sections on non-Debug).
	/// On GCC/Clang this only has effect when compile produced
	/// -ffunction-sections -fdata-sections (the Release default).
	/// </summary>
	public bool? EnableDeadCodeElimination { get; private set; }

	/// <summary>
	/// Native dead-code-elimination flag(s); empty when unset.
	/// </summary>
	public abstract IEnumerable<string> DeadCodeEliminationFlags { get; }

	/// <summary>
	/// Force enable/disable dead-code elimination. Modules call this from
	/// <see cref="CppModuleRule.AdditionLinkArgs"/> (e.g. to keep unreferenced
	/// symbols for reflection, or to enable it in Debug).
	/// </summary>
	public void SetEnableDeadCodeElimination(bool enable)
	{
		EnableDeadCodeElimination = enable;
	}

	/// <summary>
	/// COMDAT folding override (merge identical functions/data). <c>null</c>
	/// means use the toolchain default (MSVC: /OPT:ICF on non-Debug; GCC/Clang:
	/// not applied — no portable equivalent across ld/lld).
	/// </summary>
	public bool? EnableCOMDATFolding { get; private set; }

	/// <summary>
	/// Native COMDAT-folding flag(s); empty when unset or unsupported.
	/// </summary>
	public abstract IEnumerable<string> COMDATFoldingFlags { get; }

	/// <summary>
	/// Force enable/disable COMDAT folding. Modules call this from
	/// <see cref="CppModuleRule.AdditionLinkArgs"/>.
	/// </summary>
	public void SetEnableCOMDATFolding(bool enable)
	{
		EnableCOMDATFolding = enable;
	}

	/// <summary>
	/// Symbol stripping override (remove symbol tables from the binary to
	/// reduce size). <c>null</c> means use the toolchain default (MSVC: N/A,
	/// uses PDB; GCC/Clang: -Wl,-s on non-Debug).
	/// </summary>
	public bool? StripSymbols { get; private set; }

	/// <summary>
	/// Native strip flag(s); empty when unset or unsupported (MSVC).
	/// </summary>
	public abstract IEnumerable<string> StripSymbolsFlags { get; }

	/// <summary>
	/// Force enable/disable symbol stripping. Modules call this from
	/// <see cref="CppModuleRule.AdditionLinkArgs"/> (e.g. to keep symbols for
	/// crash symbolication in Release).
	/// </summary>
	public void SetStripSymbols(bool enable)
	{
		StripSymbols = enable;
	}

	/// <summary>
	/// Expands the high-level link options above into native flags. These are
	/// emitted alongside whatever the builder accumulated via
	/// <see cref="IArgsBuilder.Append"/> (DisableWarnings/SetLto/SetWarnAsError
	/// etc.). Note: <see cref="SetFastLink"/> is intentionally NOT expanded here
	/// — it's read directly off <c>MSVCLinkArgsBuilder.EnableFastLink</c> in the
	/// toolchain's DefaultLinkFlags.
	/// </summary>
	public override IEnumerable<string> GetAllArguments()
	{
		foreach (var argument in base.GetAllArguments())
		{
			yield return argument;
		}

		foreach (var subsystemFlag in SubsystemFlags)
		{
			yield return subsystemFlag;
		}
		foreach (var defFlag in ModuleDefinitionFlags)
		{
			yield return defFlag;
		}
		foreach (var manifestFlag in ManifestFlags)
		{
			yield return manifestFlag;
		}
		foreach (var incrementalFlag in IncrementalLinkFlags)
		{
			yield return incrementalFlag;
		}
		foreach (var dceFlag in DeadCodeEliminationFlags)
		{
			yield return dceFlag;
		}
		foreach (var icfFlag in COMDATFoldingFlags)
		{
			yield return icfFlag;
		}
		foreach (var stripFlag in StripSymbolsFlags)
		{
			yield return stripFlag;
		}
	}
}

public abstract class IArchiveArgsBuilder : IArgsBuilder
{
	public abstract void SetLto(bool enable);
}