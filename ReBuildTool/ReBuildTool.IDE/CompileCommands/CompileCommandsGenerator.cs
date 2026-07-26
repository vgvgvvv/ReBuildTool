using System.Text.Encodings.Web;
using System.Text.Json;

using NiceIO;

using ReBuildTool.Service.CompileService;
using ReBuildTool.ToolChain;

using ResetCore.Common;

namespace ReBuildTool.IDE.CompileCommands;

/// <summary>
/// Emits a JSON Compilation Database (compile_commands.json) at the project root so editors
/// (clangd, VS Code C/C++ &amp; clangd extensions, CLion) get syntax highlighting and
/// go-to-definition without needing CMake or a configure step. The entries are the exact
/// per-file compiler invocations rbt itself would run - see <see cref="CppBuilder.CollectCompileCommands"/>.
/// </summary>
public static class CompileCommandsGenerator
{
	public static void Generate(ICppSourceProviderInterface source)
	{
		var builder = new CppBuilder();
		builder.SetSource(source);
		var entries = builder.CollectCompileCommands();

			// Property names Directory/File/Arguments/Output become directory/file/arguments/output,
			// matching the LLVM JSON Compilation Database spec.
			var options = new JsonSerializerOptions
			{
				WriteIndented = true,
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				// Compile flags contain '+' (c++latest) and backslashes; the default encoder
				// escapes these. Relaxed encoding keeps the file human-readable while staying
				// valid JSON.
				Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
			};
		var json = JsonSerializer.Serialize(entries, options);

		var outputPath = source.ProjectRoot.Combine("compile_commands.json");
		outputPath.WriteAllText(json);
		Log.Info($"Generated compile_commands.json at {outputPath} ({entries.Count} entries)");
	}
}
