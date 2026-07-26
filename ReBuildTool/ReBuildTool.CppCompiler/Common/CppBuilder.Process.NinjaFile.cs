using NiceIO;
using ReBuildTool.Service.CompileService;
using ReBuildTool.Service.Global;
using ReBuildTool.ToolChain.Windows;
using ResetCore.Common;

namespace ReBuildTool.ToolChain;

public partial class CppBuilder
{
    public partial class CompileProcess
    {
        /// <summary>
        /// Generates a <c>build.ninja</c> describing this module's compile + (link|archive)
        /// steps and returns its path. Mirrors <see cref="GenerateMakeFile"/>: it reuses the
        /// same Prepare*/Collect* helpers so the command lines emitted into build.ninja are
        /// byte-identical to what the direct-compile and Makefile paths run.
        /// <para>
        /// The Ninja runner (<see cref="SDK.Ninja.Ninja.RunNinja"/>) is the one that actually
        /// executes the edges; this method only writes the file.
        /// </para>
        /// </summary>
        public NPath GenerateNinjaFile()
        {
            var generator = new NinjaFileGenerator();
            // Register all three rules up front so the build edges can reference them
            // regardless of module type (keeps FlushToFile's rule section complete even
            // if, e.g., an executable module emits no archive edges).
            var cxxRule = generator.EnsureRule(NinjaFileGenerator.CxxRuleName);
            generator.EnsureRule(NinjaFileGenerator.LinkRuleName);
            generator.EnsureRule(NinjaFileGenerator.ArchiveRuleName);

            // Configure compiler-driven dependency discovery (Phase 2). The cxx rule gets
            // either `depfile = $out.d` + `deps = gcc` (GCC/Clang: a .d file written by
            // -MMD -MF), or `deps = msvc` + a top-level msvc_deps_prefix (MSVC/clang-cl:
            // /showIncludes lines parsed from stdout). Resolve which trait to use from the
            // toolchain family; unknown toolchains fall back to no depfile (ninja still
            // rebuilds on $in changes — just not on transitively-included headers).
            ConfigureDepTracking(generator, cxxRule);

            CollectCompileEdges(generator);
            if (Module.TargetBuildType != BuildType.StaticLibrary)
            {
                CollectLinkEdge(generator);
            }
            else
            {
                CollectArchiveEdge(generator);
            }

            var resultPath = NinjaCachePath();
            generator.FlushToFile(resultPath);
            return resultPath;
        }

        /// <summary>
        /// Picks the ninja dependency-discovery trait for <see cref="NinjaFileGenerator.CxxRuleName"/>
        /// based on the active toolchain family, and wires the matching compiler flags via
        /// <see cref="CppCompilationUnit.DependencyFilePath"/> (which the toolchains' CompileArgsFor
        /// consume). Call before <see cref="CollectCompileEdges"/> so the flags are baked into the
        /// emitted command lines.
        /// </summary>
        private void ConfigureDepTracking(NinjaFileGenerator generator, NinjaFileGenerator.Rule cxxRule)
        {
            switch (ToolChain.Name)
            {
                case "MSVC":
                case "Clang" when ToolChain is WindowsClangToolchain:
                    // cl.exe / clang-cl: /showIncludes prints one line per included header,
                    // prefixed with a LOCALE-DEPENDENT string. English cl emits
                    // "Note: including file:" but localized builds emit translations
                    // (e.g. zh-CN: "注意: 包含文件:"). VSLANG=1033 (set in MSVCToolChain.EnvVars)
                    // is *supposed* to force English but is unreliable across cl versions, so
                    // we probe the actual prefix once here and bake it into msvc_deps_prefix.
                    // That makes dependency tracking work regardless of the host locale.
                    cxxRule.Deps = "msvc";
                    generator.MsvcDepsPrefix = ProbeMsvcShowIncludesPrefix();
                    break;
                case "Gcc":
                case "Clang":
                    // GCC-like drivers: -MMD -MF $out.d writes a make-format dep file next
                    // to the object; ninja's `deps = gcc` trait reads and transates it. The
                    // `$out.d` template is expanded per-edge by ninja using each edge's $out.
                    cxxRule.Deps = "gcc";
                    cxxRule.Depfile = "$out.d";
                    break;
                default:
                    // No depfile support — leave Deps/Depfile null. ninja falls back to its
                    // default behaviour (rebuild only on explicit $in changes).
                    break;
            }
        }

        /// <summary>
        /// Runs a throwaway compile through the MSVC-family compiler with /showIncludes and
        /// returns the exact locale-dependent prefix it emits before each header path. Falls
        /// back to the English "Note: including file:" if the probe fails (compiler missing,
        /// output parse error, etc.) — that keeps the build working on English systems and
        /// surfaces a warning so the failure is visible rather than silent.
        /// <para>
        /// The probe feeds cl a one-line source that <c>#include</c>s a header in a temp dir
        /// we control, then finds the emitted line that ends with that header's name. The
        /// prefix is everything before the header path on that line.
        /// </para>
        /// </summary>
        private string ProbeMsvcShowIncludesPrefix()
        {
            const string fallback = "Note: including file:";
            try
            {
                // Resolve the compiler via the toolchain so we get cl.exe (or clang-cl.exe)
                // with its full path, and inherit the toolchain env (MSVC PATH + VSLANG) so
                // the compiler and its runtime DLLs resolve exactly as in a real compile.
                var probeSrc = new NPath(Path.GetTempFileName()).ChangeExtension(".cpp");
                var probeHeader = new NPath(Path.GetTempFileName()).ChangeExtension(".h");
                const string headerToken = "rbt_showincludes_probe.h";
                // Use a recognizable sentinel name so we can find the matching /showIncludes
                // line even among system headers cl also prints.
                var sentinel = new NPath(Path.GetTempPath()).Combine(headerToken);
                File.WriteAllText(sentinel, "// probe\n");
                File.WriteAllText(probeSrc, $"#include \"{headerToken}\"\nint main(){{return 0;}}\n");

                var compiler = ToolChain.CompilerExecutableFor(probeSrc);
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = compiler.ToString(),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetTempPath(),
                };
                // /c + /showIncludes, /Fo pointing at a throwaway object, /I to the temp dir.
                // /nologo suppresses the banner; /EHsc avoids warnings; /Fo NUL avoids writing an obj.
                // ArgumentList auto-quotes each token, so pass clean unquoted values
                // (a token with a manually-added " would be double-quoted and break when
                // the temp path or username contains a space).
                psi.ArgumentList.Add("/nologo");
                psi.ArgumentList.Add("/c");
                psi.ArgumentList.Add("/showIncludes");
                psi.ArgumentList.Add("/I" + Path.GetTempPath());
                psi.ArgumentList.Add("/Fonul");
                psi.ArgumentList.Add(probeSrc.ToString());
                foreach (var (k, v) in ToolChain.EnvVars())
                {
                    psi.Environment[k] = v;
                }

                using var proc = System.Diagnostics.Process.Start(psi);
                if (proc == null)
                {
                    return fallback;
                }
                var stdout = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();
                // Drain stderr so the child can't block on a full pipe (we don't need it).
                proc.StandardError.ReadToEnd();

                // Find the /showIncludes line for our sentinel header. Its absolute path is
                // <temp>\rbt_showincludes_probe.h — match on the filename to be path-separator
                // and trailing-whitespace agnostic.
                var sentinelFileName = Path.GetFileName(sentinel.ToString());
                foreach (var line in stdout.Split('\n'))
                {
                    var trimmed = line.TrimEnd('\r');
                    // /showIncludes lines look like:  "<prefix>  C:\...\rbt_showincludes_probe.h"
                    // The path is at the end; the prefix is everything before it.
                    var idx = trimmed.IndexOf(sentinelFileName, StringComparison.OrdinalIgnoreCase);
                    if (idx <= 0)
                    {
                        continue;
                    }
                    // Walk back past the header's directory portion to find where the prefix ends.
                    // The path component immediately before the filename is its directory; the
                    // prefix is everything up to (and excluding) the start of that absolute path.
                    var dirEnd = trimmed.LastIndexOfAny(new[] { '\\', '/' }, idx - 1);
                    if (dirEnd <= 0)
                    {
                        continue;
                    }
                    // Find the start of the absolute path (first char after whitespace before dirEnd).
                    var pathStart = dirEnd;
                    while (pathStart > 0 && !char.IsWhiteSpace(trimmed[pathStart - 1]))
                    {
                        pathStart--;
                    }
                    var prefix = trimmed.Substring(0, pathStart).TrimEnd();
                    if (prefix.Length > 0)
                    {
                        return prefix;
                    }
                }
                Log.Warning($"MSVC /showIncludes prefix probe did not find sentinel header in output; " +
                            $"falling back to English \"{fallback}\". Dependency tracking may be incomplete " +
                            $"on localized cl.exe. Probe output:\n{stdout}");
            }
            catch (Exception e)
            {
                Log.Warning($"MSVC /showIncludes prefix probe failed ({e.Message}); falling back to " +
                            $"English \"{fallback}\". Dependency tracking may be incomplete on localized cl.exe.");
            }
            finally
            {
                // Best-effort cleanup of temp files; ignore failures.
                try { File.Delete(Path.Combine(Path.GetTempPath(), "rbt_showincludes_probe.h")); } catch { }
            }
            return fallback;
        }

        private void CollectCompileEdges(NinjaFileGenerator generator)
        {
            // Glob sources and build the compile units. This must happen before we set
            // DependencyFilePath below, since the units are created here.
            if (!CollectCompileUnit())
            {
                throw new Exception("can't collect compile unit");
            }

            // Arm each compile unit with a depfile path so the toolchain emits dependency
            // info. Skipped for assembly sources: ml.exe/nasm/gcc-as don't honor these
            // flags and would warn or error. The cxx rule's dep trait (gcc/msvc) is only
            // effective on the edges that actually produce a depfile, so skipping .asm
            // units here also keeps ninja from looking for a nonexistent .d for them.
            bool trackDeps = !string.IsNullOrEmpty(generator.EnsureRule(NinjaFileGenerator.CxxRuleName).Deps);
            if (trackDeps)
            {
                foreach (var unit in CompileUnits)
                {
                    if (unit.SourceFile.ExtensionWithDot == ".asm")
                    {
                        continue;
                    }
                    // Place the depfile next to the object: foo.obj -> foo.obj.d,
                    // foo.o -> foo.o.d. ChangeExtension replaces the trailing extension
                    // (.obj/.o) with "<that ext>.d", which appends .d rather than stripping.
                    var objExt = unit.OutputFile.ExtensionWithDot; // ".obj" or ".o"
                    unit.DependencyFilePath = unit.OutputFile.ChangeExtension(objExt + ".d");
                }
            }

            // Now build the invocations — CompileArgsFor will see DependencyFilePath and
            // emit -MMD/-MF (gcc) or /showIncludes (msvc). We intentionally do NOT call
            // FilterUpToDateInvocations: ninja's own incremental logic is the source of
            // truth here, driven by the depfiles above.
            if (!CollectCompileInvocations())
            {
                throw new Exception("can't collect compile invocations");
            }

            foreach (var invocation in CompileInvocation)
            {
                var unit = invocation.Unit;
                var edge = new NinjaFileGenerator.Edge(
                    NinjaFileGenerator.NinjaPath(unit.OutputFile),
                    NinjaFileGenerator.CxxRuleName);

                // $in = the source file (ninja requires explicit inputs).
                edge.WithInput(NinjaFileGenerator.NinjaPath(unit.SourceFile));

                // Header dependencies are NOT listed manually: the cxx rule's deps trait
                // (gcc/msvc) makes ninja pull them from the compiler-generated depfile /
                // /showIncludes output, which is recursive and macro-conditional aware —
                // a strict improvement over the old single-level GetAllCompileUnitDep scan.
                // For .asm units (no depfile produced) ninja simply falls back to $in only.

                // Per-edge command variables. Ninja runs `command = $cc $args` through a shell
                // (sh -c / cmd /c), so variable values must be SHELL-quoted — ninja's own
                // `$ ` space escaping only protects paths in the structural `build` line, not
                // in variable values passed to the shell. ProgramName and Arguments are clean
                // argv tokens, so ShellQuote.ForArgument wraps each one (same as the Makefile
                // path); the `build` line's output/inputs above stay NinjaPath-escaped.
                edge.WithVariable("cc", ShellQuote.ForProgram(invocation.ProgramName.ToString()));
                edge.WithVariable("args",
                    string.Join(' ', invocation.Arguments.Select(ShellQuote.ForArgument)));

                generator.Edges.Add(edge);
            }
        }

        private void CollectLinkEdge(NinjaFileGenerator generator)
        {
            // PrepareLinkUnit writes the .rsp (object files only) and
            // PrepareLinkInvocation builds the link.exe/clang++ command that references
            // it via @rsp — both reused unchanged from the direct-compile path.
            if (!PrepareLinkUnit())
            {
                throw new Exception("PrepareLinkUnit failed.");
            }
            if (!PrepareLinkInvocation())
            {
                throw new Exception("PrepareLinkInvocation failed.");
            }

            var unit = LinkInvocation.Unit;
            var edge = new NinjaFileGenerator.Edge(
                NinjaFileGenerator.NinjaPath(unit.OutputPath),
                NinjaFileGenerator.LinkRuleName);

            // Object files are explicit inputs (rebuild output when any obj changes).
            foreach (var objFile in unit.ObjectFiles)
            {
                edge.WithInput(NinjaFileGenerator.NinjaPath(objFile));
            }

            // Library deps: only order-only (|), mirroring CollectLinkTarget. We don't
            // want a touched third-party .lib to force a relink of a binary whose own
            // object files haven't changed, but ninja still needs them in the graph so
            // the link step can see them. Existing-on-disk filter + ArgumentException
            // guard copied verbatim from CollectLinkTarget.
            foreach (var libraryPath in unit.LibraryPaths)
            {
                foreach (var library in unit.DynamicLibraries)
                {
                    NPath libPath;
                    try { libPath = libraryPath.Combine(library); }
                    catch (ArgumentException e)
                    {
                        Log.Warning($"Ninja link dep scan: skipping dynamic lib \"{library}\" under \"{libraryPath}\": {e.Message}");
                        continue;
                    }
                    if (libPath.Exists())
                    {
                        edge.WithImplicitInput(NinjaFileGenerator.NinjaPath(libPath));
                    }
                }
                foreach (var library in unit.StaticLibraries)
                {
                    NPath libPath;
                    try { libPath = libraryPath.Combine(library); }
                    catch (ArgumentException e)
                    {
                        Log.Warning($"Ninja link dep scan: skipping static lib \"{library}\" under \"{libraryPath}\": {e.Message}");
                        continue;
                    }
                    if (libPath.Exists())
                    {
                        edge.WithImplicitInput(NinjaFileGenerator.NinjaPath(libPath));
                    }
                }
            }

            edge.WithVariable("cc", ShellQuote.ForProgram(LinkInvocation.ProgramName.ToString()));
            edge.WithVariable("args",
                string.Join(' ', LinkInvocation.Arguments.Select(ShellQuote.ForArgument)));

            generator.Edges.Add(edge);
        }

        private void CollectArchiveEdge(NinjaFileGenerator generator)
        {
            if (!PrepareArchiveUnit())
            {
                throw new Exception("PrepareArchiveUnit failed.");
            }
            if (!PrepareArchiveInvocation())
            {
                throw new Exception("PrepareArchiveInvocation failed.");
            }

            var unit = ArchiveInvocation.Unit;
            var edge = new NinjaFileGenerator.Edge(
                NinjaFileGenerator.NinjaPath(unit.OutputPath),
                NinjaFileGenerator.ArchiveRuleName);

            foreach (var objFile in unit.ObjectFiles)
            {
                edge.WithInput(NinjaFileGenerator.NinjaPath(objFile));
            }

            // Static archive deps: order-only, existing-on-disk only (matches
            // CollectArchiveTarget, minus that path's missing try/catch — we keep
            // the guard for safety since the same malformed-name failure mode exists).
            foreach (var libraryPath in unit.LibraryPaths)
            {
                foreach (var library in unit.StaticLibraries)
                {
                    NPath libPath;
                    try { libPath = libraryPath.Combine(library); }
                    catch (ArgumentException e)
                    {
                        Log.Warning($"Ninja archive dep scan: skipping static lib \"{library}\" under \"{libraryPath}\": {e.Message}");
                        continue;
                    }
                    if (libPath.Exists())
                    {
                        edge.WithImplicitInput(NinjaFileGenerator.NinjaPath(libPath));
                    }
                }
            }

            edge.WithVariable("cc", ShellQuote.ForProgram(ArchiveInvocation.ProgramName.ToString()));
            edge.WithVariable("args",
                string.Join(' ', ArchiveInvocation.Arguments.Select(ShellQuote.ForArgument)));

            generator.Edges.Add(edge);
        }

        /// <summary>
        /// <c>&lt;ProjectRoot&gt;/Intermedia/&lt;Platform&gt;/&lt;Config&gt;/&lt;Arch&gt;/NinjaCache/&lt;Module&gt;/build.ninja</c>.
        /// Mirrors <see cref="MakeFileCachePath"/> with NinjaCache/build.ninja in place
        /// of MakeFileCache/Makefile.
        /// </summary>
        private NPath NinjaCachePath()
        {
            var result = Source.ProjectRoot
                .Combine("Intermedia", IPlatformSupport.CurrentTargetPlatform.ToString())
                .Combine(Options.Configuration.ToString())
                .Combine(Options.Architecture.Name)
                .Combine("NinjaCache", Module.TargetName, "build.ninja");
            result.EnsureParentDirectoryExists();
            return result;
        }
    }
}
