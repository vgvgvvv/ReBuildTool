# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [v0.0.3] - 2026-07-29

### Added

#### IDE / Project Generation
- **VSCode project generation** — new `VSCode` IDE project type alongside Visual Studio and CMake, with generated tasks that forward `--TargetPlatform` and cross-compile flags, and modules set up before being read.
- **`compile_commands.json`** — emitted for IDE code highlighting and go-to-definition jump.
- **CMake build routed through `rbt`** — CMake build now drives `rbt` under the hood while keeping native IDE IntelliSense, mapping the IDE's `$<CONFIG>` selection to `rbt --BuildConfig`.
- **Target platform propagation** — VS-generated NMake commands now forward `--TargetPlatform`; ARM platform names are correctly mapped to `rbt --TargetArch` CLI tokens.
- **Executable subprojects** — subprojects can now build as executables.

#### C/C++ Compiler / Build Backends
- **Ninja build backend** — new backend with compiler-driven dependency scanning.
- **Link-time optimization controls** — DCE / ICF / strip toggles exposed on the toolchain.
- **ArgsBuilder** — implemented missing compile/link capability methods.
- **RTTI / Exception toggles** — `SetEnableRTTI` / `SetEnableException` exposed to module rules.
- **Parallel + incremental build** — restored native parallel compile and incremental builds; Windows now defaults to direct-compile mode.
- **Configurable parallelism** — makefile `-j` job count configurable via `--MaxJobs`.
- **Source filtering API** — `SourceFiles` / `ExcludeDirectories` / `ExcludeFiles`, plus filtering of source files by platform-named directories.

#### Logging
- **Thread-safe concurrent logging** — logger is now concurrency-safe; `Clean` preserves the `Logs/` directory.
- **Build log file** — build logs are also written to `Intermedia/Logs/Build.log`.

#### Header Tool / Bootstrap
- **Unattended `ResetHeaderTool` bootstrap** — `rbt` can bootstrap `ResetHeaderTool` on its own; on Windows the bootstrap builds only the tool itself.

#### CI
- **Per-commit CI** — the project is now built and tested on Windows, Linux, and macOS for every commit.

#### Samples
- New sample projects for static/dynamic libraries and multi-module dependency chains.

### Changed

- **Removed legacy INI project configuration** — the `.module.ini` / `.target.ini` configuration system has been removed in favor of C# rule files; orphaned INI files were cleaned up from Sample projects, and README / HowToUse docs updated accordingly.
- **Module rules declared in `Setup`** — module rules are now declared inside `Setup` instead of captured via a state snapshot.
- **Argument quoting centralized** — shell argument quoting moved out of the toolchains into per-consumer escape layers, with platform-correct quoting and per-format escaping.

### Fixed

- Distinguish C vs C++ compiler and flags; fix Linux linker invocation.
- **GNU ld link order** — pass object files before libraries so linking resolves correctly.
- Carry generation-time build args into the `rbt` custom target for CMake.
- Call `Setup` directly in `SetupAllModules` to avoid wiping build module state (VS project setup no longer pollutes build modules).
- **Module rule state across re-setup** — restore a module rule's declared state (including framework module paths) on re-setup.
- MSVC defaults to C++latest and drops the `UNICODE` defines; `.inl` files excluded from compilable sources.
- Avoid duplicate `projectType` and empty `dllSearchPath` in the header tool.
- Tolerate non-relative paths and `NPath` serialization errors; ignore invalid include/source paths.
- Fix `.sln` filter bug and Android include path.
- Log skipped entries in MakeFile/HeaderTool error handlers instead of failing silently.
- Fix C++ plugin setup ordering relative to module setup.

## [v0.0.2] - 2026-07-26

Re-release of v0.0.1 (same commit) to fix an upload failure in the release pipeline.

## [v0.0.1] - 2026-07-08

First release.
