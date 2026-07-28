# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## English

### [v0.0.3] - 2026-07-29

#### Added

**IDE / Project Generation**
- **VSCode project generation** — new `VSCode` IDE project type alongside Visual Studio and CMake, with generated tasks that forward `--TargetPlatform` and cross-compile flags, and modules set up before being read.
- **`compile_commands.json`** — emitted for IDE code highlighting and go-to-definition jump.
- **CMake build routed through `rbt`** — CMake build now drives `rbt` under the hood while keeping native IDE IntelliSense, mapping the IDE's `$<CONFIG>` selection to `rbt --BuildConfig`.
- **Target platform propagation** — VS-generated NMake commands now forward `--TargetPlatform`; ARM platform names are correctly mapped to `rbt --TargetArch` CLI tokens.
- **Executable subprojects** — subprojects can now build as executables.

**C/C++ Compiler / Build Backends**
- **Ninja build backend** — new backend with compiler-driven dependency scanning.
- **Link-time optimization controls** — DCE / ICF / strip toggles exposed on the toolchain.
- **ArgsBuilder** — implemented missing compile/link capability methods.
- **RTTI / Exception toggles** — `SetEnableRTTI` / `SetEnableException` exposed to module rules.
- **Parallel + incremental build** — restored native parallel compile and incremental builds; Windows now defaults to direct-compile mode.
- **Configurable parallelism** — makefile `-j` job count configurable via `--MaxJobs`.
- **Source filtering API** — `SourceFiles` / `ExcludeDirectories` / `ExcludeFiles`, plus filtering of source files by platform-named directories.

**Logging**
- **Thread-safe concurrent logging** — logger is now concurrency-safe; `Clean` preserves the `Logs/` directory.
- **Build log file** — build logs are also written to `Intermedia/Logs/Build.log`.

**Header Tool / Bootstrap**
- **Unattended `ResetHeaderTool` bootstrap** — `rbt` can bootstrap `ResetHeaderTool` on its own; on Windows the bootstrap builds only the tool itself.

**CI**
- **Per-commit CI** — the project is now built and tested on Windows, Linux, and macOS for every commit.

**Samples**
- New sample projects for static/dynamic libraries and multi-module dependency chains.

#### Changed

- **Removed legacy INI project configuration** — the `.module.ini` / `.target.ini` configuration system has been removed in favor of C# rule files; orphaned INI files were cleaned up from Sample projects, and README / HowToUse docs updated accordingly.
- **Module rules declared in `Setup`** — module rules are now declared inside `Setup` instead of captured via a state snapshot.
- **Argument quoting centralized** — shell argument quoting moved out of the toolchains into per-consumer escape layers, with platform-correct quoting and per-format escaping.

#### Fixed

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

### [v0.0.2] - 2026-07-26

Re-release of v0.0.1 (same commit) to fix an upload failure in the release pipeline.

### [v0.0.1] - 2026-07-08

First release.

---

## 中文

### [v0.0.3] - 2026-07-29

#### 新增

**IDE 与工程生成**
- **VSCode 工程生成** —— 新增 `VSCode` IDE 工程生成类型（与 Visual Studio、CMake 并列）。生成的 VSCode 任务会转发 `--TargetPlatform` 与交叉编译参数，并在读取模块前先完成模块的 setup。
- **`compile_commands.json`** —— 生成该文件，供 IDE 做代码高亮与跳转定义。
- **CMake 构建改走 `rbt`** —— CMake 构建底层改由 `rbt` 驱动，同时保留 IDE 原生 IntelliSense；将 IDE 的 `$<CONFIG>` 选择映射为 `rbt --BuildConfig`。
- **目标平台透传** —— VS 生成的 NMake 命令现在会转发 `--TargetPlatform`；ARM 平台名称被正确映射为 `rbt --TargetArch` 的命令行参数。
- **可执行子工程** —— 子工程现在支持构建为可执行程序。

**C/C++ 编译器与构建后端**
- **Ninja 构建后端** —— 新增后端，依赖扫描由编译器驱动。
- **链接期优化控制** —— 工具链暴露 DCE / ICF / strip 开关。
- **ArgsBuilder** —— 实现了此前缺失的编译/链接能力方法。
- **RTTI / 异常开关** —— 模块规则新增 `SetEnableRTTI` / `SetEnableException`。
- **并行与增量构建** —— 恢复原生并行编译与增量构建；Windows 默认采用直接编译模式。
- **可配置并发数** —— makefile 的 `-j` 并发数可通过 `--MaxJobs` 配置。
- **源码过滤 API** —— 新增 `SourceFiles` / `ExcludeDirectories` / `ExcludeFiles`；并支持按平台命名的目录过滤源码文件。

**日志**
- **线程安全的并发日志** —— 日志改为线程安全，支持并发；执行 `Clean` 时会保留 `Logs/` 目录。
- **构建日志文件** —— 构建日志同时写入 `Intermedia/Logs/Build.log`。

**头文件工具与自举**
- **无人值守的 `ResetHeaderTool` 自举** —— `rbt` 现在可自行完成 `ResetHeaderTool` 的自举；在 Windows 上自举时只编译该工具本身。

**持续集成（CI）**
- **按提交触发的 CI** —— 每次提交都会在 Windows、Linux、macOS 三平台构建并测试。

**示例工程**
- 新增静态库/动态库以及多模块依赖链的示例工程。

#### 变更

- **移除遗留的 INI 工程配置** —— 移除 `.module.ini` / `.target.ini` 配置体系，改用 C# 规则文件；清理 Sample 中残留的 INI 文件，并相应更新 README 与 HowToUse 文档。
- **模块规则在 `Setup` 内声明** —— 模块规则改为在 `Setup` 内声明，不再通过状态快照捕获。
- **参数转义集中化** —— Shell 参数转义从工具链中抽离，下沉到各使用方的转义层；实现平台正确的引号处理与按格式的转义。

#### 修复

- 区分 C 与 C++ 的编译器和编译参数，修复 Linux 链接器调用。
- **GNU ld 链接顺序** —— 目标文件（.o）排在库之前，确保符号正确解析。
- CMake 生成期的构建参数被带入 `rbt` 的 custom target。
- `SetupAllModules` 中直接调用 `Setup`，避免清空 build module 状态（VS 工程的 setup 不再污染 build module）。
- **重新 setup 时的模块规则状态** —— 正确还原模块规则已声明的状态（包括框架模块的路径）。
- MSVC 默认使用 C++latest 并移除 `UNICODE` 宏定义；`.inl` 文件不再作为可编译源码参与编译。
- 修复头文件工具中 `projectType` 重复与 `dllSearchPath` 为空的问题。
- 容忍非相对路径与 `NPath` 序列化错误，忽略无效的 include/源码路径。
- 修复 `.sln` 过滤器 bug 与 Android 的 include 路径。
- MakeFile/HeaderTool 的错误处理改为记录被跳过的条目，不再静默失败。
- 修复 C++ 插件 setup 与 module setup 的先后顺序问题。

### [v0.0.2] - 2026-07-26

v0.0.1 的重新发布（同一提交），用于修复发布流水线中上传失败的问题。

### [v0.0.1] - 2026-07-08

首次发布。
