﻿using NiceIO;
using ResetCore.Common;

namespace ReBuildTool.ToolChain;

public abstract partial class ClangToolChain
{
	internal override CppArchiveInvocation MakeArchiveInvocation(CppArchiveUnit cppArchiveUnit)
	{
		var invocation = new CppArchiveInvocation(cppArchiveUnit);
		invocation.ProgramName = ClangSdk.GetArchiver();
		invocation.EnvVars.AddRange(EnvVars());
		invocation.Arguments.AddRange(ArchiveArgsFor(cppArchiveUnit));
		return invocation;
	}

	protected virtual IEnumerable<string> ArchiveArgsFor(CppArchiveUnit unit)
	{
		yield return $"-o";
        yield return unit.OutputPath.InQuotes();
        
        foreach (var defaultLinkFlag in DefaultArchiveFlags(unit))
        {
            yield return defaultLinkFlag;
        }
        
        yield return "@" + unit.ResponseFile.InQuotes();
	}
	
	protected IEnumerable<string> DefaultArchiveFlags(CppArchiveUnit cppArchiveUnit)
	{
		var linkBuilder = cppArchiveUnit.ArchiveArgsBuilder;
        
		foreach (var argument in linkBuilder.GetAllArguments())
		{
			yield return argument;
		}
		
		foreach (var staticLibrary in cppArchiveUnit.StaticLibraries)
		{
			yield return staticLibrary.ToNPath().InQuotes();
		}
		
		foreach (var libraryPath in cppArchiveUnit.LibraryPaths)
		{
			yield return $"-L{libraryPath.InQuotes()}";
		}
        
		foreach (var libpath in ToolChainLibraryPaths())
		{
			yield return $"-L{libpath.InQuotes()}";
		}
	}

}