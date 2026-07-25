namespace ReBuildTool.ToolChain;

public abstract class Architecture
{
	public abstract	int Bit { get; }

	/// <summary>
	/// Display name shown in IDEs (e.g. the VS Platform dropdown: "x86", "x64",
	/// "ARMv7", "ARM64"). This is NOT necessarily what rbt's <c>--TargetArch</c>
	/// parser accepts - use <see cref="CommandLineName"/> for that.
	/// </summary>
	public abstract string Name { get; }

	/// <summary>
	/// The lowercase token rbt's <c>--TargetArch</c> parser accepts
	/// (<see cref="BuildOptions.CreateDefault"/>): x86 / x64 / arm32 / arm64.
	/// Used when emitting the rbt invocation (VS NMake commands); mirrors the
	/// accepted values documented on <c>CppCompilerArgs.TargetArch</c>.
	/// </summary>
	public abstract string CommandLineName { get; }

	public abstract int MaximumAlignment { get; }
	
	public static bool operator==(Architecture left, Architecture right)
	{
		if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
			return ReferenceEquals(left, right);

		return left.GetType() == right.GetType();
	}

	public static bool operator!=(Architecture left, Architecture right)
	{
		return !(left == right);
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
			return false;

		return GetType() == obj.GetType();
	}

	public override int GetHashCode()
	{
		return GetType().GetHashCode();
	}
}

public class x86Architecture : Architecture
{
	public override int Bit => 32;

	public override string Name => "x86";

	public override string CommandLineName => "x86";

	public override int MaximumAlignment => 32;
}

public class x64Architecture : Architecture
{
	public override int Bit => 64;

	public override string Name => "x64";

	public override string CommandLineName => "x64";

	public override int MaximumAlignment => 32;
}

public class ARMv7Architecture : Architecture
{
	public override int Bit => 32;

	public override string Name => "ARMv7";

	public override string CommandLineName => "arm32";

	public override int MaximumAlignment => 8;
}

public class ARM64Architecture : Architecture
{
	public override int Bit => 64;

	public override string Name => "ARM64";

	public override string CommandLineName => "arm64";

	public override int MaximumAlignment => 16;
}