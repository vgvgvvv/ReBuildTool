﻿using NiceIO;
using ReBuildTool.Service.CompileService;
using ReBuildTool.Service.Global;

namespace ReBuildTool.ToolChain;

public abstract partial class ModuleRule
{
    internal static void GenerateCode(IModuleInterface module, NPath outputDirectory)
    {
        var moduleDirectory = outputDirectory.Combine("Modules", module.GetType().Name);
        moduleDirectory.EnsureDirectoryExists();
        var publicDirectory = moduleDirectory.Combine("Public").EnsureDirectoryExists();
        var privateDirectory = moduleDirectory.Combine("Private").EnsureDirectoryExists();
        module.PublicIncludePaths.Add(publicDirectory);
        module.SourceDirectories.Add(publicDirectory);
        module.SourceDirectories.Add(privateDirectory);
        var moduleInternalName = $"{module.GetType().Name}.internal";
        File.WriteAllText(publicDirectory.Combine($"{moduleInternalName}.h"), GenerateHeader(module));
        File.WriteAllText(privateDirectory.Combine($"{moduleInternalName}.cpp"), GenerateSource(module));
    }

    private static string GenerateHeader(IModuleInterface module)
    {
        var moduleInternalName = $"{module.GetType().Name}.internal";
        var targetNameUpper = module.TargetName.ToUpper();
        SourceCodeBuilder builder = new();
        builder.AppendLine("// This file is auto-generated by ReBuildTool");
        builder.AppendLine("#pragma once\n\n");
        
        builder.AppendLine($"#ifdef {targetNameUpper}_BUILT_AS_STATIC");
        builder.AppendLine($"#  define {targetNameUpper}_API");
        builder.AppendLine($"#else");
        builder.AppendLine($"#  ifdef COMPILER_MSVC");
        builder.AppendLine($"#      ifdef {targetNameUpper}_EXPORTS");
        builder.AppendLine($"#          define {targetNameUpper}_API __declspec(dllexport)");
        builder.AppendLine($"#      else");
        builder.AppendLine($"#          define {targetNameUpper}_API __declspec(dllimport)");
        builder.AppendLine($"#      endif");
        builder.AppendLine($"#  elif COMPILER_GCC");
        builder.AppendLine($"#      ifdef {targetNameUpper}_EXPORTS");
        builder.AppendLine($"#          define {targetNameUpper}_API __attribute__((visibility(\"default\")))");
        builder.AppendLine($"#      else");
        builder.AppendLine($"#          define {targetNameUpper}_API");
        builder.AppendLine($"#      endif");
        builder.AppendLine($"#  elif COMPILER_CLANG");
        builder.AppendLine($"#      ifdef {targetNameUpper}_EXPORTS");
        builder.AppendLine($"#          define {targetNameUpper}_API __attribute__((visibility(\"default\")))");
        builder.AppendLine($"#      else");
        builder.AppendLine($"#          define {targetNameUpper}_API");
        builder.AppendLine($"#      endif");
        builder.AppendLine($"#  endif");
        builder.AppendLine("#endif\n\n");
        return builder.ToString();
    }
    
    private static string GenerateSource(IModuleInterface module)
    {
        var moduleInternalName = $"{module.GetType().Name}.internal";
        SourceCodeBuilder builder = new();
        builder.AppendLine("// This file is auto-generated by ReBuildTool");
        builder.AppendLine($"#include \"{moduleInternalName}.h\"\n\n");
        return builder.ToString();
    }
}