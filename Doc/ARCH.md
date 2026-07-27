# ReBuildTool Architecture

This document describes the internal architecture of ReBuildTool (`rbt`): how the
solution is organized, how a build flows from the command line to compiled
binaries, and the key abstractions each layer provides. It is aimed at
contributors — for end-user usage see [HowToUse.md](HowToUse.md) and
[Whats-ReBuildTool.md](Whats-ReBuildTool.md).

> 相关文档：[中文架构说明](ARCH.zh-CN.md) · [HowToUse.md](HowToUse.md) · [中文使用指南](HowToUse.zh-CN.md)

---

## 1. What it is

ReBuildTool is a C++ build system written in **.NET 8**, inspired by Unreal
Build Tool. Rather than hand-written makefiles, a project describes itself with
**C# rule files** (`*.target.cs` / `*.module.cs`). `rbt` compiles those rule
files into an assembly at runtime, loads them reflectively, and uses them to
drive per-platform toolchains (MSVC / GCC / Clang / NDK / Wasm). It can either
compile directly or emit IDE projects (Visual Studio `.sln` / CMake) that route
their builds back through `rbt`.

---

## 2. Solution layout

The solution (`ReBuildTool/ReBuildTool.sln`) contains the following projects.
Arrows in the dependency graph point from a project to what it **depends on**.

| Project | Role |
| --- | --- |
| **ReBuildTool** | CLI entry point (`Program.cs`). Parses args, wires up the `ServiceContext`, runs the selected `RunMode` over each project. |
| **ReBuildTool.Service** | Orchestration layer. Defines the project/compile/IDE service interfaces and the `ServiceContext` service locator. The seam every other layer plugs into. |
| **ReBuildTool.CppCompiler** | The core. Parses C++ build rules, resolves modules/targets, drives compile + link + archive through platform toolchains and SDKs. ~9k LOC. |
| **ReBuildTool.CSharpCompiler** | Compiles the user's `*.target.cs` / `*.module.cs` rule files into a `CompileRules.dll` at runtime (Roslyn-based). |
| **ReBuildTool.IDE** | Generators that emit Visual Studio (`.vcxproj`/`.sln`) and CMake projects. |
| ~~**ReBuildTool.Ini**~~ | Removed. `Program.cs` no longer creates an `IIniProject`; `ServiceContext.Config.cs::InitFromIni()` remains as a stub that returns `false`. |
| **ReBuildTool.Common** | Shared utilities: `Shell` (process wrapper), `NiceIO` path helpers, misc. |
| **ReBuildTool.Updater** | Standalone self-update executable, shipped next to `rbt`. |
| **ReBuildTool.CppCompiler.Standalone** | Thin standalone host around the C++ compiler for isolated runs/testing. |
| **ReBuildTool.Test** | NUnit integration tests that build the sample projects under `Sample/`. |
| **ReBuildTool.LuaWrapper** / **ReBuildTool.ToolChain** | Currently empty placeholders reserved for future work. |

External dependencies come from the two git submodules under `Vendor/`:
`ReCSharpCommon` (`ResetCore.Common` — logging, `CmdParser`, `Result<T>`,
`Singleton`) and `UniToLua`.

### Dependency direction

```
        ReBuildTool (CLI)
          │  depends on
          ▼
   ┌──────────────┬───────────────┬──────────────┐
   ▼              ▼               ▼              ▼
CppCompiler   CSharpCompiler     IDE           Ini
   │              │               │              │
   └──────────────┴───────┬───────┴──────────────┘
                          ▼
                       Service
                          │
                          ▼
                        Common
                          │
                          ▼
                Vendor/ReCSharpCommon
```

`Service` deliberately sits below the compilers and knows them only through
interfaces (`ICppProject`, `IIniProject`, `IGenerateIDEProjService`, …).
Concrete implementations live in the compiler/IDE projects and are registered
into the `ServiceContext` at startup — this is how the CLI stays decoupled from
any specific toolchain.

---

## 3. The service-locator seam (`ServiceContext`)

`ReBuildTool.Service.Context.ServiceContext` is a `Singleton` holding a
`TypeMap` from interface type → concrete type. Interfaces that can be resolved
implement the marker `IProvideByService`.

- `RegisterType<TInterface, TImpl>()` populates the map (defaults live in
  `ServiceContext.Default.cs`; an INI file can override via `InitFromIni`).
- `Create<T>(args…)` reflectively finds a matching constructor and returns a
  `Result<T>`.

This lets `Program.cs` create an `ICppProject` / `IIniProject` without
referencing their implementations directly, and lets tests or config swap
implementations.

---

## 4. Build rule model (the UBT-style core idea)

A user's C++ project is described by C# rule files placed under `Source/`:

- `*.target.cs` → a class extending **`CppTargetRule`** (what to build: an
  executable/library, which modules, configuration).
- `*.module.cs` → a class extending **`CppModuleRule`** (a compilation unit:
  source/include/exclude dirs, defines, compile flags, dependencies, RTTI /
  exceptions toggles, Unity/jumbo options via `UnityModuleRule`).

The extensions are constants in `ICppProject`
(`ReBuildTool.Service/CompileService/CppCompile.cs`):
`.target.cs` and `.module.cs`.

These rule files are **not** compiled ahead of time. At runtime
`CppBuildProject.ParseRules()`:

1. Globs all `*.target.cs` / `*.module.cs` under `Source/`.
2. Builds an `IAssemblyCompileUnit`, adding the rule files as sources and
   referencing the compiler assemblies (so rules can use `CppModuleRule`, the
   platform helpers, etc.).
3. Uses **ReBuildTool.CSharpCompiler** to emit `CompileRules.dll` into
   `Intermedia/`.
4. Loads that assembly with `Assembly.LoadFile` and instantiates every
   `CppTargetRule` / `CppModuleRule` it finds into `TargetRules` / `ModuleRules`.

`NeedReBuildRuleAssembly()` compares timestamps so the rule DLL is only
recompiled when a rule file (or `rbt` itself) is newer — with a bounded retry
counter for load failures.

### 4.1 Package management and why it runs first

`CppBuildProject.Parse()` calls `RestorePackages()` **before** `ParseRules()`.
That ordering is forced by the step above: `CompileRules.dll` is loaded exactly
once with `Assembly.LoadFile` and .NET cannot unload it, so there is no way to
compile rules, discover a dependency, and then add its rules to the same
assembly. Every package's `.module.cs` therefore has to be on disk before the
glob runs.

That constraint is also why the manifest is JSON rather than C#: resolving
transitive dependencies means repeatedly reading the manifest of a package that
has not been downloaded yet, which a plain file read does trivially and a
compile-and-load cycle cannot.

```
Parse()
  ├─ RestorePackages()                    IPackageService (ReBuildTool.Service/PackageService)
  │    ├─ read <ProjectRoot>/RBTPackage.json        (absent → returns immediately, zero cost)
  │    ├─ PackageResolver: depth-first walk
  │    │     fetch → read the package's own manifest → recurse
  │    │     exact pins only; conflicting pins and cycles are hard errors
  │    ├─ IPackageFetcher per source      Git (clone/fetch/reset)
  │    │                                  HttpArchive (download, sha256, unpack)
  │    │                                  Path (used in place)
  │    │                                  Vcpkg (install, then describe as binary)
  │    └─ write RBTPackage.lock.json      only when changed
  ├─ PackageModuleBinder                  synthesizes a rule for a binary package;
  │                                       installs a consumer-supplied overlay rule
  └─ ParseRules()                         globs Source/ + package roots + generated rules
```

Key types in `ReBuildTool.Service/PackageService/`: `PackageManifest`,
`PackageLockFile`, `PackageResolver`, `PackageRestoreService`, and
`Fetchers/IPackageFetcher`. The download / extract / SHA256 helpers the archive
fetcher needs live in `ReBuildTool.Common/Misc/` (`Downloader`,
`ArchiveExtractor`, `Hashing`) — the first networking in rbt that is not a
shell-out to git.

Above them, in `ReBuildTool.CppCompiler/Package/`: `PackageModuleBinder`
generates a module rule for a prebuilt binary package and installs a
consumer-supplied `overlay` rule, and `PackageArtifactSelector` picks the right
prebuilt artifact at `Setup` time. Generating a rule file, rather than
registering an `IModuleInterface` straight into `ModuleRules`, keeps everything
downstream working unchanged: the `ModuleRulePaths` lookup in `InitAllRule`
(which throws for a module it has no path for), the `SetupInternal` lifecycle,
the `_API` macro codegen, all four IDE generators and the HeaderTool plugin.

The `--Offline` / `--ForceRestore` / `--UpdateLock`
flags live in `ReBuildTool.CppCompiler/Project/PackageArgs.cs` — deliberately in
that assembly rather than beside the service, because `CmdParser` discovers
argument groups by scanning `AppDomain.CurrentDomain.GetAssemblies()` and .NET
loads assemblies lazily.

Packages contribute **modules and extensions, never targets**: a `*.target.cs`
inside a package is ignored, so what gets built stays the consuming project's
decision. Restored packages land in `<ProjectRoot>/Packages/`, not under
`Intermedia/` — see §8.

`CppTargetRule.GitLibraries` is the superseded predecessor of all this. It is
marked `[Obsolete]` and never read; it could not have worked, for the ordering
reason above.

---

## 5. End-to-end build flow

```
rbt --Mode Build --Target MyGame --ProjectRoot <dir>
        │
        ▼
Program.cs
  ├─ CmdParser.Parse<Program>()          parse CLI args
  ├─ ServiceContext.Create<IIniProject>() + Create<ICppProject>()
  └─ for each project: Parse() → dispatch on RunMode
                                  │
             ┌────────────────────┼─────────────────────┐
          Init                 Build/ReBuild            Clean
        Setup()              Build(target)             Clean()
                                  │
                                  ▼
                    CppBuildProject.Build(target)
                      1. LoadRuleAssembly()   compile+load CompileRules.dll (§4)
                      2. resolve TargetRule + its ModuleRules
                      3. select IToolChain for the target platform/arch
                      4. CppBuilder drives the module graph:
                            CollectCompileUnit  → source globbing + platform/exclude filtering
                            CollectCompileInvocations → per-file compiler command
                            FilterUpToDate      → skip .obj newer than source+headers
                            RunCompileInvocations → Parallel.ForEach (‑‑MaxJobs)
                            Link / Archive      → produce exe / .so / .a / .lib
```

`RunMode` is defined in `ICommonCommandGroup`: `Init`, `Build`, `Clean`,
`ReBuild`.

### CppBuilder internals

`CppBuilder` (in `ReBuildTool.CppCompiler/Common/`) is split across partial
files by concern:

- `CppBuilder.Process.Compile.cs` — collect compile units, incremental
  up-to-date filtering, parallel compile.
- `CppBuilder.Process.Link.cs` — link stage.
- `CppBuilder.Process.MakeFile.cs` — alternative path that emits a makefile and
  delegates incremental logic to `nmake`/`make -jN`.

Two compile paths exist: **direct** (rbt itself schedules and runs each
compiler invocation in parallel and does its own timestamp-based incremental
skipping) and **makefile** (rbt writes a makefile and lets the make tool handle
scheduling/incrementality).

---

## 6. Toolchains, SDKs and platform support

`ReBuildTool.CppCompiler` isolates every compiler-specific detail behind
`IToolChain` (`ToolChain/IToolChain.cs`). A toolchain knows its file extensions
(`.obj`/`.o`, `.exe`, `.a`/`.lib`, `.so`/`.dll`/`.dylib`) and how to build
compile/link/archive invocations.

```
ToolChain/
  MSVC/     Windows MSVC (cl / link / lib)
  Gcc/      GCC
  Clang/    Windows, Linux, macOS (XCode), Android (NDK) variants
  Wasm/     WebAssembly — placeholder, mostly NotImplemented
```

Each concrete toolchain is further split into partial files:
`*.Compile.cs`, `*.Link.cs`, `*.Archive.cs`, `*.ArgsBuilder.cs` — mirroring the
`IToolChain` responsibilities. Command-line argument assembly lives in the
`ArgsBuilder` partials.

**SDK discovery** lives under `SDK/` (e.g. `SDK/MSVC/MSVC.cs` locates a Visual
Studio install via COM; `WindowsSDK.cs`, `LinuxSDK.cs`, `XCode.cs`, the NDK
Clang SDK). SDKs supply include/lib paths and environment variables that the
toolchains inject into each `Shell` invocation.

### Process execution

All external tools run through `ReBuildTool.Common.Misc.Shell`, a fluent
wrapper over `System.Diagnostics.Process`
(`WithProgram().WithArguments().WithEnvVars().Execute().WaitForEnd()`). It
redirects stdout/stderr into the logger and, because parallel compiles share a
non-thread-safe logger, serializes output with a static lock.

### Third-party / codegen support

`ThirdpartSupport/` hosts optional pipeline plugins:

- **HeaderTool** — a reflection/codegen pass over C++ headers (analogous to
  UHT), producing generated sources fed back into the compile set.
- **Unity** — unity/jumbo build support (`UnityNativePluginSupport`,
  `UnityModuleRule`).

---

## 7. IDE project generation

Instead of compiling, `rbt` can emit IDE projects via
`IGenerateIDEProjService` (`ProjectGenType` = `VisualStudio` | `CMake` |
`VSCode` | `CompileCommands`):

- **ReBuildTool.IDE / VisualStudio** — generates `.vcxproj` (with
  configuration/filter/user partials in `VCProject.*.cs`) and a `.sln`. The
  generated NMake project shells the build back out to `rbt` while keeping IDE
  IntelliSense working.
- **ReBuildTool.IDE / CMake** — generates `CMakeLists` whose custom target
  invokes `rbt`, forwarding the IDE's `$<CONFIG>`, target platform and
  architecture into the corresponding `rbt --BuildConfig / --TargetPlatform /
  --TargetArch` arguments.
- **ReBuildTool.IDE / VSCode** — generates a `.vscode/` folder (`tasks.json`,
  `launch.json`, `c_cpp_properties.json`) at the project root. Build / rebuild /
  clean tasks invoke `rbt` — pinning `--TargetPlatform` and replaying the other
  flags of the generating `rbt` invocation (`--TargetArch`, `--NDKRoot`, ...) so
  a cross-compiled project keeps building for its target platform — each
  executable module gets a run task plus a cppvsdbg/cppdbg debug launch
  configuration (with a build preLaunchTask), and IntelliSense reads the
  `compile_commands.json` `rbt` emits.
- **ReBuildTool.IDE / CompileCommands** — emits a `compile_commands.json` (JSON
  Compilation Database) at the project root directly from the exact per-file
  compiler invocations `rbt` would run (`CppBuilder.CollectCompileCommands`), so
  clangd / VS Code / CLion get code highlighting and go-to-definition with no
  CMake install or configure step. It is written for **every** project type
  (alongside the VS/CMake output) and can be requested on its own via
  `--IDEProjectType CompileCommands`.

This "IDE drives rbt" design keeps a single source of build truth: the IDE is a
front end, the actual compilation always goes through the same rule/toolchain
path described above.

---

## 8. Intermediate & output layout

Build artifacts are written under the project's `Intermedia/` tree, keyed by
platform / configuration / architecture, e.g.:

```
<ProjectRoot>/Intermedia/
  Logs/Build.log
  CompileRules.dll                          compiled rule assembly (§4)
  <Platform>/<Config>/<Arch>/ObjectCache/   per-source .obj/.o mirror of Source/
```

Restored packages deliberately sit **outside** that tree:

```
<ProjectRoot>/
  RBTPackage.json                           dependency manifest, hand written
  RBTPackage.lock.json                      resolved commits, generated, commit it
  Packages/<name>/                          materialized package (git clone, or absent
                                            for a path dependency, which is used in place)
```

`Packages/` is not under `Intermedia/` because `Clean()` empties that directory,
and `CleanIfNeed()` triggers a clean on its own whenever the rbt binaries are
newer than the last build — every dependency would be re-downloaded after each
rebuild and each rbt update. Restore adds `/Packages/` to the project's
`.gitignore`.

The `ObjectCache` mirrors the source tree so incremental timestamp checks
(`IsCompileUnitUpToDate`) can map each source file to its object deterministically.

---

## 9. Distribution & updates

- **Install**: `BuildScript/Install.sh` / `Install.bat` clone the repo to
  `~/.rbt` (or `%USERPROFILE%\.rbt`), build all binaries, and add them to
  `PATH`.
- **Release**: `.github/workflows/release.yml` publishes self-contained
  single-folder builds of `ReBuildTool` and `ReBuildTool.Updater` for
  win-x64 / osx-x64 / osx-arm64 / linux-x64 on every `v*` tag.
- **Update**: `ReBuildTool.Updater` ships alongside `rbt` to perform in-place
  self-updates.

---

## 10. Where to start reading

| To understand… | Start at |
| --- | --- |
| CLI dispatch | `ReBuildTool/Program.cs` |
| Service wiring | `ReBuildTool.Service/Context/ServiceContext*.cs` |
| Rule compile + load | `ReBuildTool.CppCompiler/Project/CppBuildProject.cs` |
| Package restore / resolution | `ReBuildTool.Service/PackageService/` (start at `PackageResolver.cs`) |
| Compile scheduling / incremental | `ReBuildTool.CppCompiler/Common/CppBuilder.Process.Compile.cs` |
| Adding a toolchain | `ReBuildTool.CppCompiler/ToolChain/IToolChain.cs` + an existing `ToolChain/<Name>/` |
| SDK discovery | `ReBuildTool.CppCompiler/SDK/` |
| IDE generation | `ReBuildTool.IDE/` + `ReBuildTool.Service/IDEService/` |
| Process execution | `ReBuildTool.Common/Misc/Shell.cs` |
