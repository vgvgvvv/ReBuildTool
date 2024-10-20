﻿using NiceIO;

using ReBuildTool.CppCompiler;
using ReBuildTool.Service.Global;
using ReBuildTool.ToolChain.Android;

namespace ReBuildTool.ToolChain;

public class AndroidPlatformSupport : IPlatformSupport
{
	public override bool Supports(RuntimePlatform platform)
	{
		return platform is AndroidRuntimePlatform;
	}

	public override IToolChain MakeCppToolChain(Architecture architecture, BuildConfiguration buildConfiguration)
	{
		var androidArgs = AndroidCompilerArgs.Get();
		var ndkHome = Environment.GetEnvironmentVariable("NDK_HOME");
		if (string.IsNullOrEmpty(ndkHome))
		{
			ndkHome = Environment.GetEnvironmentVariable("NDK_ROOT");
		}
		if (string.IsNullOrEmpty(ndkHome))
		{
			ndkHome = Environment.GetEnvironmentVariable("ANDROID_NDK_HOME");
		}
		if (!string.IsNullOrEmpty(androidArgs.NDKRoot))
		{
			ndkHome = androidArgs.NDKRoot;
		}

		if (string.IsNullOrEmpty(ndkHome))
		{
			throw new Exception("cannot find NDK location");
		}
		
		return new AndroidClangToolchain(buildConfiguration, architecture, ndkHome.ToNPath());
	}
}