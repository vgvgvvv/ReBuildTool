namespace ReBuildTool.ToolChain;

public abstract class Architecture
{
	public abstract	int Bit { get; }

	public abstract string Name { get; }
	
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

	public override int MaximumAlignment => 32;
}

public class x64Architecture : Architecture
{
	public override int Bit => 64;

	public override string Name => "x64";

	public override int MaximumAlignment => 32;
}

public class ARMv7Architecture : Architecture
{
	public override int Bit => 32;

	public override string Name => "ARMv7";

	public override int MaximumAlignment => 8;
}

public class ARM64Architecture : Architecture
{
	public override int Bit => 64;

	public override string Name => "ARM64";

	public override int MaximumAlignment => 16;
}