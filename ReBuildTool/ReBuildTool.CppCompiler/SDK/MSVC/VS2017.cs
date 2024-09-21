﻿using NiceIO;

namespace ReBuildTool.ToolChain.SDK;

internal class Msvc15 : MsvcSDK
{
	public static MsvcSDK Create(Version version, NPath path)
	{
		return new Msvc15(version, path);
	}

	public override IEnumerable<NPath> GetSdkIncludeDirectories()
	{
		throw new NotImplementedException();
	}

	public override IEnumerable<NPath> GetSdkLibraryDirectories()
	{
		throw new NotImplementedException();
	}

	public override IEnumerable<NPath> GetVcIncludeDirectories()
	{
		throw new NotImplementedException();
	}

	public override IEnumerable<NPath> GetVcLibraryDirectories()
	{
		throw new NotImplementedException();
	}

	public override NPath GetVcToolRootPath()
	{
		throw new NotImplementedException();
	}

	public override NPath CompilerPath
	{
		get;
	}

	public override NPath LinkerPath
	{
		get;
	}

	public override NPath ArchiverPath
	{
		get;
	}

	public override NPath AsmCompilerPath
	{
		get;
	}

	public override IEnumerable<string> PathEnvironmentVariable
	{
		get;
	}

	protected Msvc15(Version version, NPath path) : base(version, path)
	{
	}
	

}