# ReBuildTool 架构说明

本文档介绍 ReBuildTool（`rbt`）的内部架构：解决方案如何组织、一次构建如何从命令行流转到最终二进制产物，以及每一层提供的关键抽象。面向贡献者阅读——终端用户使用说明请见
[HowToUse.zh-CN.md](HowToUse.zh-CN.md) 与 [Whats-ReBuildTool.md](Whats-ReBuildTool.md)。

> 相关文档：[English Architecture](ARCH.md) · [中文使用指南](HowToUse.zh-CN.md)

---

## 1. 这是什么

ReBuildTool 是一个用 **.NET 8** 编写的 C++ 构建系统，设计思路借鉴 Unreal Build Tool。
项目不通过手写 makefile 描述自身，而是用 **C# 规则文件**（`*.target.cs` / `*.module.cs`）来描述。
`rbt` 在运行时把这些规则文件编译成程序集，通过反射加载，再用它们驱动各平台工具链
（MSVC / GCC / Clang / NDK / Wasm）。它既可以直接编译，也可以生成 IDE 工程
（Visual Studio `.sln` / CMake），而这些工程的构建会再次回调 `rbt`。

---

## 2. 解决方案结构

解决方案（`ReBuildTool/ReBuildTool.sln`）包含以下项目。依赖图中的箭头由某项目指向它所**依赖**的项目。

| 项目 | 职责 |
| --- | --- |
| **ReBuildTool** | CLI 入口（`Program.cs`）。解析参数、装配 `ServiceContext`，按选定的 `RunMode` 对每个 project 执行。 |
| **ReBuildTool.Service** | 编排层。定义 project / compile / IDE 各服务接口以及 `ServiceContext` 服务定位器，是其他各层接入的接缝。 |
| **ReBuildTool.CppCompiler** | 核心。解析 C++ 构建规则、解析 module/target，通过各平台工具链与 SDK 驱动 compile + link + archive，约 9k 行。 |
| **ReBuildTool.CSharpCompiler** | 在运行时把用户的 `*.target.cs` / `*.module.cs` 规则文件编译为 `CompileRules.dll`（基于 Roslyn）。 |
| **ReBuildTool.IDE** | 生成器，输出 Visual Studio（`.vcxproj`/`.sln`）与 CMake 工程。 |
| ~~**ReBuildTool.Ini**~~ | 已移除。`Program.cs` 不再创建 `IIniProject`；`ServiceContext.Config.cs::InitFromIni()` 仅作为返回 `false` 的桩保留。 |
| **ReBuildTool.Common** | 共享工具：`Shell`（进程封装）、`NiceIO` 路径辅助等。 |
| **ReBuildTool.Updater** | 独立的自更新可执行文件，与 `rbt` 一起分发。 |
| **ReBuildTool.CppCompiler.Standalone** | 围绕 C++ 编译器的轻量独立宿主，用于隔离运行 / 测试。 |
| **ReBuildTool.Test** | NUnit 集成测试，构建 `Sample/` 下的示例工程。 |
| **ReBuildTool.LuaWrapper** / **ReBuildTool.ToolChain** | 目前为空的占位项目，预留给后续功能。 |

外部依赖来自 `Vendor/` 下的两个 git 子模块：`ReCSharpCommon`
（`ResetCore.Common`——日志、`CmdParser`、`Result<T>`、`Singleton`）与 `UniToLua`。

### 依赖方向

```
        ReBuildTool (CLI)
          │  依赖
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

`Service` 刻意位于各编译器之下，只通过接口（`ICppProject`、`IIniProject`、
`IGenerateIDEProjService` 等）认识它们。具体实现位于 compiler / IDE 项目中，在启动时
注册进 `ServiceContext`——这正是 CLI 得以与任何具体工具链解耦的方式。

---

## 3. 服务定位接缝（`ServiceContext`）

`ReBuildTool.Service.Context.ServiceContext` 是一个 `Singleton`，持有一张
从「接口类型 → 具体类型」的 `TypeMap`。可被解析的接口需实现标记接口 `IProvideByService`。

- `RegisterType<TInterface, TImpl>()` 填充映射表（默认注册见 `ServiceContext.Default.cs`；
  也可通过 INI 文件由 `InitFromIni` 覆盖）。
- `Create<T>(args…)` 通过反射查找匹配的构造函数，返回 `Result<T>`。

这样 `Program.cs` 就能在不直接引用实现类的情况下创建 `ICppProject` / `IIniProject`，
测试或配置也能替换实现。

---

## 4. 构建规则模型（UBT 风格的核心思想）

用户的 C++ 工程通过放在 `Source/` 下的 C# 规则文件来描述：

- `*.target.cs` → 继承 **`CppTargetRule`** 的类（构建什么：可执行文件 / 库、包含哪些 module、配置）。
- `*.module.cs` → 继承 **`CppModuleRule`** 的类（一个编译单元：源码 / 头文件 / 排除目录、
  宏定义、编译标志、依赖、RTTI / 异常开关，以及通过 `UnityModuleRule` 提供的 Unity/jumbo 选项）。

扩展名是 `ICppProject`（`ReBuildTool.Service/CompileService/CppCompile.cs`）中的常量：
`.target.cs` 与 `.module.cs`。

这些规则文件**不是**预先编译的。运行时由 `CppBuildProject.ParseRules()` 完成：

1. 递归匹配 `Source/` 下所有 `*.target.cs` / `*.module.cs`。
2. 构造一个 `IAssemblyCompileUnit`，把规则文件加入源列表，并引用编译器程序集
   （使规则能使用 `CppModuleRule`、平台辅助类等）。
3. 用 **ReBuildTool.CSharpCompiler** 将 `CompileRules.dll` 输出到 `Intermedia/`。
4. 用 `Assembly.LoadFile` 加载该程序集，实例化其中所有 `CppTargetRule` / `CppModuleRule`，
   分别存入 `TargetRules` / `ModuleRules`。

`NeedReBuildRuleAssembly()` 通过比较时间戳，仅在某个规则文件（或 `rbt` 本身）更新时才重新
编译规则 DLL——并对加载失败设有次数上限的重试。

### 4.1 包管理，以及它为什么必须跑在最前面

`CppBuildProject.Parse()` 会在 `ParseRules()` **之前**调用 `RestorePackages()`。
这个顺序是被上一节的机制强制的：`CompileRules.dll` 用 `Assembly.LoadFile` 只加载一次，
.NET 无法卸载它，因此不存在「先编译规则、再发现依赖、再把依赖的规则加进同一个程序集」
这条路。所有包的 `.module.cs` 都必须在 glob 之前就位于磁盘上。

同样的约束决定了清单格式必须是 JSON 而不是 C#：解析传递依赖意味着反复读取「尚未下载的包」
的清单，普通文件读取轻而易举，而「编译 + 加载」的循环做不到。

```
Parse()
  ├─ RestorePackages()                    IPackageService（ReBuildTool.Service/PackageService）
  │    ├─ 读取 <ProjectRoot>/RBTPackage.json      （不存在则立即返回，零开销）
  │    ├─ PackageResolver：深度优先遍历
  │    │     拉取 → 读取该包自己的清单 → 递归
  │    │     只接受精确 pin；pin 冲突与依赖成环均为硬错误
  │    ├─ 按来源分派 IPackageFetcher      Git（clone/fetch/reset）
  │    │                                  HttpArchive（下载、sha256 校验、解压）
  │    │                                  Path（原地使用）
  │    └─ 写出 RBTPackage.lock.json      仅在内容变化时
  ├─ PackageModuleBinder                  为二进制包合成规则；
  │                                       安装消费方提供的 overlay 规则
  └─ ParseRules()                         glob Source/ + 包根目录 + 生成的规则目录
```

主要类型在 `ReBuildTool.Service/PackageService/` 下：`PackageManifest`、
`PackageLockFile`、`PackageResolver`、`PackageRestoreService`，以及
`Fetchers/IPackageFetcher`。压缩包 fetcher 需要的下载 / 解压 / SHA256 三个辅助类放在
`ReBuildTool.Common/Misc/`（`Downloader`、`ArchiveExtractor`、`Hashing`）——
这是 rbt 里第一处不靠 shell 调用 git 的网络访问。

在它们之上，`ReBuildTool.CppCompiler/Package/` 里：`PackageModuleBinder` 负责为预编译
二进制包生成模块规则、并安装消费方提供的 `overlay` 规则；`PackageArtifactSelector` 负责在
`Setup` 阶段挑选正确的预编译产物。选择「生成规则文件」而不是「直接把 `IModuleInterface`
注册进 `ModuleRules`」，是为了让下游的一切原样继续工作：`InitAllRule` 里的
`ModuleRulePaths` 查找（找不到路径就抛异常）、`SetupInternal` 生命周期、`_API` 宏代码生成、
四种 IDE 生成器，以及 HeaderTool 插件。`--Offline` / `--ForceRestore` / `--UpdateLock` 三个参数
定义在 `ReBuildTool.CppCompiler/Project/PackageArgs.cs` —— 特意放在该程序集而不是服务旁边，
因为 `CmdParser` 通过扫描 `AppDomain.CurrentDomain.GetAssemblies()` 发现参数组，
而 .NET 的程序集是惰性加载的。

包提供的是**模块和 extension，绝不是 target**：包里的 `*.target.cs` 会被忽略，
构建什么始终由消费方项目决定。restore 出来的包放在 `<ProjectRoot>/Packages/`，
不在 `Intermedia/` 下 —— 见 §8。

`CppTargetRule.GitLibraries` 是这套机制被取代掉的前身。它已标记 `[Obsolete]` 且从未被读取；
基于上面的顺序原因，它本来也不可能工作。

---

## 5. 端到端构建流程

```
rbt --Mode Build --Target MyGame --ProjectRoot <dir>
        │
        ▼
Program.cs
  ├─ CmdParser.Parse<Program>()          解析 CLI 参数
  ├─ ServiceContext.Create<IIniProject>() + Create<ICppProject>()
  └─ 对每个 project：Parse() → 按 RunMode 分派
                                  │
             ┌────────────────────┼─────────────────────┐
          Init                 Build/ReBuild            Clean
        Setup()              Build(target)             Clean()
                                  │
                                  ▼
                    CppBuildProject.Build(target)
                      1. LoadRuleAssembly()   编译+加载 CompileRules.dll（§4）
                      2. 解析 TargetRule 及其 ModuleRules
                      3. 为目标平台/架构选择 IToolChain
                      4. CppBuilder 驱动 module 图：
                            CollectCompileUnit  → 源码 glob + 平台/排除过滤
                            CollectCompileInvocations → 逐文件生成编译命令
                            FilterUpToDate      → 跳过比源码+头文件更新的 .obj
                            RunCompileInvocations → Parallel.ForEach（--MaxJobs）
                            Link / Archive      → 产出 exe / .so / .a / .lib
```

`RunMode` 在 `ICommonCommandGroup` 中定义：`Init`、`Build`、`Clean`、`ReBuild`。

### CppBuilder 内部结构

`CppBuilder`（位于 `ReBuildTool.CppCompiler/Common/`）按关注点拆分为多个 partial 文件：

- `CppBuilder.Process.Compile.cs`——收集编译单元、增量的 up-to-date 过滤、并行编译。
- `CppBuilder.Process.Link.cs`——链接阶段。
- `CppBuilder.Process.MakeFile.cs`——另一条路径：生成 makefile，把增量逻辑委托给
  `nmake`/`make -jN`。

存在两条编译路径：**直接式**（rbt 自己调度并并行运行每条编译命令，并基于时间戳做增量跳过）与
**makefile 式**（rbt 写出 makefile，由 make 工具负责调度 / 增量）。

---

## 6. 工具链、SDK 与平台支持

`ReBuildTool.CppCompiler` 把所有编译器相关细节隔离在 `IToolChain`
（`ToolChain/IToolChain.cs`）之后。工具链知道自己的文件扩展名
（`.obj`/`.o`、`.exe`、`.a`/`.lib`、`.so`/`.dll`/`.dylib`），以及如何构造
compile/link/archive 命令。

```
ToolChain/
  MSVC/     Windows MSVC（cl / link / lib）
  Gcc/      GCC
  Clang/    Windows、Linux、macOS（XCode）、Android（NDK）各变体
  Wasm/     WebAssembly——占位，大多为 NotImplemented
```

每个具体工具链进一步拆为 partial 文件：`*.Compile.cs`、`*.Link.cs`、`*.Archive.cs`、
`*.ArgsBuilder.cs`——与 `IToolChain` 的职责一一对应。命令行参数的拼装位于 `ArgsBuilder` partial 中。

**SDK 探测**位于 `SDK/`（例如 `SDK/MSVC/MSVC.cs` 通过 COM 定位 Visual Studio 安装；
`WindowsSDK.cs`、`LinuxSDK.cs`、`XCode.cs`、NDK Clang SDK）。SDK 提供 include/lib 路径与
环境变量，工具链会把它们注入每次 `Shell` 调用。

### 进程执行

所有外部工具都通过 `ReBuildTool.Common.Misc.Shell` 运行——一个对
`System.Diagnostics.Process` 的流式封装
（`WithProgram().WithArguments().WithEnvVars().Execute().WaitForEnd()`）。它把
stdout/stderr 重定向到日志，并且由于并行编译共享一个非线程安全的 logger，用一把静态锁串行化输出。

### 第三方 / 代码生成支持

`ThirdpartSupport/` 承载可选的流水线插件：

- **HeaderTool**——对 C++ 头文件做反射 / 代码生成扫描（类似 UHT），产出注入回编译集合的生成源码。
- **Unity**——unity/jumbo 构建支持（`UnityNativePluginSupport`、`UnityModuleRule`）。

---

## 7. IDE 工程生成

除直接编译外，`rbt` 还可通过 `IGenerateIDEProjService` 生成 IDE 工程
（`ProjectGenType` = `VisualStudio` | `CMake` | `VSCode` | `CompileCommands`）：

- **ReBuildTool.IDE / VisualStudio**——生成 `.vcxproj`（配置 / 过滤器 / 用户等 partial 位于
  `VCProject.*.cs`）与 `.sln`。生成的 NMake 工程把构建再 shell 回 `rbt`，同时保留 IDE 的 IntelliSense。
- **ReBuildTool.IDE / CMake**——生成 `CMakeLists`，其自定义目标调用 `rbt`，把 IDE 的
  `$<CONFIG>`、目标平台与架构分别转发为对应的 `rbt --BuildConfig / --TargetPlatform / --TargetArch` 参数。
- **ReBuildTool.IDE / VSCode**——在工程根目录生成 `.vscode/` 文件夹（`tasks.json`、
  `launch.json`、`c_cpp_properties.json`）。构建 / 重新构建 / 清理任务都调用 `rbt`，并会固定写入
  `--TargetPlatform`、同时回放生成工程时 `rbt` 命令行上的其余参数（`--TargetArch`、`--NDKRoot` 等），
  保证交叉编译的工程在 IDE 中依然按目标平台构建；每个可执行模块
  都会得到一个运行任务以及一个 cppvsdbg/cppdbg 调试启动配置（带构建 preLaunchTask），
  IntelliSense 则读取 `rbt` 生成的 `compile_commands.json`。
- **ReBuildTool.IDE / CompileCommands**——直接根据 `rbt` 实际会执行的逐文件编译命令
  （`CppBuilder.CollectCompileCommands`）在工程根目录生成 `compile_commands.json`（JSON 编译数据库），
  让 clangd / VS Code / CLion 无需安装 CMake、无需 configure 即可获得代码高亮与跳转。它对**每一种**
  工程类型都会生成（与 VS/CMake 产物一同输出），也可通过 `--IDEProjectType CompileCommands` 单独生成。

这种「IDE 驱动 rbt」的设计保证了单一的构建事实来源：IDE 只是前端，真正的编译始终走上文描述的同一条
规则 / 工具链路径。

---

## 8. 中间产物与输出布局

构建产物写入工程的 `Intermedia/` 目录树，按 平台 / 配置 / 架构 分层，例如：

```
<ProjectRoot>/Intermedia/
  Logs/Build.log
  CompileRules.dll                          编译出的规则程序集（§4）
  <Platform>/<Config>/<Arch>/ObjectCache/   Source/ 的逐源码 .obj/.o 镜像
```

`ObjectCache` 镜像源码树，使增量时间戳检查（`IsCompileUnitUpToDate`）能确定性地把每个源文件
映射到它的目标文件。

restore 出来的包特意放在这棵树**之外**：

```
<ProjectRoot>/
  RBTPackage.json                           依赖清单，手写
  RBTPackage.lock.json                      解析到的 commit，工具生成，应当提交
  Packages/<name>/                          物化后的包（git clone；path 依赖原地使用，
                                            不会出现在这里）
```

`Packages/` 不放在 `Intermedia/` 下，是因为 `Clean()` 会清空该目录，而且只要 rbt 的二进制
比上次构建新，`CleanIfNeed()` 就会自行触发一次 clean —— 那样每次 rebuild、每次 rbt 升级
都要重新下载全部依赖。restore 会把 `/Packages/` 加进项目的 `.gitignore`。

---

## 9. 分发与更新

- **安装**：`BuildScript/Install.sh` / `Install.bat` 将仓库克隆到 `~/.rbt`
  （或 `%USERPROFILE%\.rbt`），构建全部二进制并加入 `PATH`。
- **发布**：`.github/workflows/release.yml` 在每个 `v*` tag 上，为
  win-x64 / osx-x64 / osx-arm64 / linux-x64 发布自包含的单目录构建
  （`ReBuildTool` 与 `ReBuildTool.Updater`）。
- **更新**：`ReBuildTool.Updater` 与 `rbt` 一同分发，用于原地自更新。

---

## 10. 从哪里开始读

| 想理解… | 起点 |
| --- | --- |
| CLI 分派 | `ReBuildTool/Program.cs` |
| 服务装配 | `ReBuildTool.Service/Context/ServiceContext*.cs` |
| 规则编译 + 加载 | `ReBuildTool.CppCompiler/Project/CppBuildProject.cs` |
| 包 restore / 解析 | `ReBuildTool.Service/PackageService/`（从 `PackageResolver.cs` 读起） |
| 编译调度 / 增量 | `ReBuildTool.CppCompiler/Common/CppBuilder.Process.Compile.cs` |
| 新增工具链 | `ReBuildTool.CppCompiler/ToolChain/IToolChain.cs` + 已有的 `ToolChain/<Name>/` |
| SDK 探测 | `ReBuildTool.CppCompiler/SDK/` |
| IDE 生成 | `ReBuildTool.IDE/` + `ReBuildTool.Service/IDEService/` |
| 进程执行 | `ReBuildTool.Common/Misc/Shell.cs` |
