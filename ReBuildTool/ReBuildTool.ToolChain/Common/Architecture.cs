﻿namespace ReBuildTool.ToolChain.ToolChain;

public abstract class Architecture
{
	public abstract	int Bit { get; }

	public abstract string Name { get; }
	
	public abstract int MaximumAlignment { get; }
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