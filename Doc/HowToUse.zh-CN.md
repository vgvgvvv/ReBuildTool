# ReBuildTool 使用指南

ReBuildTool（简称 RBT）是一个由 C# 驱动的原生构建系统，设计思路类似于虚幻引擎的
UBT：你用一些简短的 C# 类（`.target.cs` / `.module.cs`）来描述 **Target**（目标）
和 **Module**（模块），RBT 会即时编译这些规则文件，再用它们来驱动真正的
C/C++ 工具链（MSVC、Clang、Gcc 或 Wasm），并生成 IDE 工程（Visual Studio / CMake）。

本文档介绍日常使用方式。项目整体介绍请见
[Whats-ReBuildTool.md](Whats-ReBuildTool.md)。

（English version: [HowToUse.md](HowToUse.md)）

## 1. 构建 RBT 本身

仓库中只包含源码，没有预编译好的二进制文件。使用
[BuildScript](../BuildScript) 目录下的脚本构建一次即可：

```bash
cd BuildScript
./BuildAll.sh        # Windows 下用 BuildAll.bat
./BuildUpdater.sh    # Windows 下用 BuildUpdater.bat
```

该脚本会对 `ReBuildTool.csproj` 和 `ReBuildTool.Updater.csproj` 执行
`dotnet publish`（自包含发布，基于 .NET 8），分别生成
`win-x64`、`osx-x64`、`osx-arm64`、`linux-x64` 四个平台的产物，输出到
`ReBuildTool/Binary/<OS><Arch>/...`。

构建完成后，`rbt.sh` / `rbt.bat` 和 `rbt-updater.sh` / `rbt-updater.bat`
只是简单的转发脚本，会调用当前系统/架构对应的二进制文件，并透传所有参数，例如：

```bash
./BuildScript/rbt.sh --ProjectRoot . --Mode Build --Target MyGame
```

## 2. 使用 Booster 引导一个新项目

不需要本地已有 RBT 仓库也能开始一个新项目——把
`BuildScript/RBTBooster.sh`（或 `.bat`）复制到一个空文件夹中，然后运行：

```bash
./RBTBooster.sh --init [TargetName]
```

它会做以下几件事：
1. 如果 `$RBT_HOME`（默认是 `~/.rbt`，可通过环境变量 `RBT_HOME` 覆盖）下还没有
   ReBuildTool 仓库，就克隆一份；如果已存在，则询问是否需要重新构建。
2. 用该仓库构建出 RBT 本体和 Updater（`BuildAll`/`BuildUpdater`）。
3. 在当前目录以 `Init` 模式运行刚构建出的 `ReBuildTool` 二进制文件，
   生成一份初始的 Target/Module 代码和 IDE 工程。
4. RBT 会把自己的 `RBTBooster.sh/.bat` 复制到项目旁边，并在项目根目录下
   生成两个便捷的包装脚本：`InitProject.sh/.bat` 和
   `BuildProject.sh/.bat`（分别是对 `RBTBooster --init` /
   `--build` 的简单封装）。**请不要手动修改这些脚本**——它们会被工具自动重新生成。

后续构建：

```bash
./RBTBooster.sh --build [TargetName]
# 或者，在脚本生成之后直接用：
./BuildProject.sh
```

## 3. 直接运行工具

每一次 RBT 调用最终都会归结为一次对 `ReBuildTool` 可执行文件的调用，
带上 `--Mode` 和（通常还有）`--Target`：

```bash
ReBuildTool --ProjectRoot <path> --Mode <RunMode> --Target <name> [options...]
```

| 参数 | 含义 |
|---|---|
| `--ProjectRoot <path>` | 项目根目录，默认为当前工作目录。 |
| `--Mode <RunMode>` | **必填。** 取值为 `Init`、`Build`、`Clean`、`ReBuild` 之一。 |
| `--Target <name>` | 要构建的目标名称，默认为 `ProjectRoot` 文件夹名。 |
| `--RunDry` | 空跑模式——只做解析/校验，不实际编译。 |
| `--BoosterSource <path>` | 内部参数，由 Booster 脚本设置，用于 RBT 重新生成这些脚本。请勿手动设置。 |

`Mode` 的行为（详见
[Program.cs](../ReBuildTool/ReBuildTool/Program.cs)）：
- **Init** —— 如果 `Source/` 下还没有任何 Target/Module，则生成默认的一份，
  然后生成 IDE 工程（Visual Studio 的 `.sln`，或 CMake 工程）。
- **Build** —— 编译指定的目标。
- **Clean** —— 清理构建产物。
- **ReBuild** —— 先 `Clean` 再 `Build`。

RBT 在每次运行时实际上会**并行处理两种工程类型**：基于 C# 规则的 C++ 工程
（`ICppProject`，见下文）和一种旧版的基于 INI 的工程（`IIniProject`，见第 6 节）。
两者都会被解析，并通过同一个 `Mode` 参数分发执行。

### C++ 相关的专用参数

以下参数仅在构建原生（C/C++）目标时才有意义：

| 参数 | 含义 |
|---|---|
| `--TargetPlatform` | 例如 `Windows`、`Linux`、`MacOSX`、`iOS`、`Android`、`Wasm`。 |
| `--TargetArch` | `x86` \| `x64` \| `arm32` \| `arm64`。 |
| `--BuildConfig` | `Debug` \| `Release` \| `ReleasePlus` \| `ReleaseSize`。 |
| `--UseClang` | 使用 Clang 而不是平台默认工具链（Windows 上默认是 MSVC，Linux 上默认是 Gcc/Clang）。 |
| `--ClangPath` | 指定 Clang 安装路径。 |
| `--CustomIncludeDirs`、`--CustomDefines`、`--CustomCompileFlags`、`--CustomLinkFlags`、`--CustomArchiveFlags` | 在模块自身声明的基础上追加额外的路径/参数。 |
| `--CustomStaticLibraries`、`--CustomDynamicLibraries`、`--CustomLibraryDirectories` | 额外的库文件/库搜索路径。 |
| `--CppCompilePlugins` | 要运行的 `ITargetCompilePlugin` 插件名称，用于编译前后的钩子。 |
| `--UseMakeFileBuild` | 生成并使用 Makefile 驱动构建，而不是直接调用工具链（默认 `true`）。 |
| `--DebugToolchainCmd` | 打印实际执行的工具链命令行。 |

平台专属参数组（仅在对应 `--TargetPlatform` 下生效）：
- Android：`--NDKRoot`、`--SDKRoot`、`--NDKTargetVersion`（默认 `25`）
- iOS：`--IOSTargetVersion`（默认 `15.0`）
- macOS：`--MacOSXTargetVersion`（默认 `11.5`）

支持的原生工具链：**MSVC**（自动检测 VS2017/2019/2022）、
**Clang**（Windows/Linux/macOS/iOS/Android）、**Gcc**，以及 **Wasm**。

## 4. 编写 C++ 工程 —— Target 与 Module

RBT 会在 `<ProjectRoot>/Source/` 下自动发现所有 `*.target.cs`、
`*.module.cs`、`*.extension.cs` 文件，把它们一起编译进内存中的
`CompileRules.dll`，然后通过反射遍历得到的类型。

可参考 [Sample/BuildCpp](../Sample/BuildCpp) 中的示例：
[Main.target.cs](../Sample/BuildCpp/Source/Main.target.cs) 和
[MainModule.module.cs](../Sample/BuildCpp/Source/MainModule/MainModule.module.cs)。

> **注意：** 仓库中的示例使用了较短的类名 `TargetRule` / `ModuleRule`，
> 这两个类名在当前代码库中已经不存在了。工具自身的脚手架生成逻辑
>（`--Mode Init` 时使用）生成的是 `CppTargetRule` / `CppModuleRule`——
> 新建工程时请使用这两个正确的类名。

### Target 规则（`*.target.cs`）

```csharp
using ReBuildTool.ToolChain;

public class MyGameTarget : CppTargetRule
{
    public MyGameTarget()
    {
        UsedModules.Add("MyGameModule");
    }
}
```

`CppTargetRule` 的主要成员：
- `List<string> UsedModules` —— 链接进该 Target 的模块（入口模块）。
- `List<GitLibrary> GitLibraries` —— 外部 git 依赖库（`Name`、`Url`、`Branch`）。
- `List<ITargetCompilePlugin> Plugins` —— 编译前/后钩子。
- `virtual void Setup(ICppBuildContext)` / `virtual void PostBuild()` —— 可重写以实现自定义逻辑。

### Module 规则（`*.module.cs`）

```csharp
using ReBuildTool.ToolChain;

public class MyGameModule : CppModuleRule
{
    public MyGameModule()
    {
        TargetBuildType = BuildType.StaticLibrary; // 或 DynamicLibrary / Executable
        Dependencies.Add("SomeOtherModule");
        PublicIncludePaths.Add("Public");
    }
}
```

`CppModuleRule` 的主要成员：
- `BuildType TargetBuildType` —— `StaticLibrary`（静态库）\| `DynamicLibrary`（动态库，默认）\| `Executable`（可执行文件）。
- `List<string> Dependencies` —— 该模块依赖的其他模块。
- 成对出现的 Public/Private 属性：`Private...` 只影响本模块，`Public...`
  还会传递给依赖它的所有模块，包括：
  `IncludePaths`、`Defines`、`CompileFlags`、`LinkFlags`、`ArchiveFlags`、
  `StaticLibraries`、`DynamicLibraries`、`LibraryDirectories`。
- `List<string> SourceDirectories` —— 模块文件夹之外的额外源码目录。
- `bool IsSupport` —— 可重写以按平台开启/关闭某个模块。
- 针对单个编译单元的钩子：`CompileFlagsFor`、`DefinesFor`、`IncludePathsFor(CppCompilationUnit)`。
- 构建参数钩子：`AdditionCompileArgs`、`AdditionLinkArgs`、`AdditionArchiveArgs`。

`UnityModuleRule`（`CppModuleRule` 的子类）用于 Unity/Jumbo 合批构建，
会自动生成 `<Module>.internal.h/.cpp` 这一对导入/导出宏文件。

### 预期的目录结构

`--Mode Init` 会生成（并期望）如下结构：

```
<ProjectRoot>/
  Source/
    MyGameTarget.target.cs
    Src/
      MyGame/
        MyGameModule.module.cs
        Public/
          MyGameModule.h
        Private/
          MyGameModule.cpp
```

## 5. 编程式 / 生命周期 API

无论是 C++ 工程还是 INI 工程，都暴露相同的生命周期方法，
`Program.cs` 中的模式分发逻辑，以及
[ReBuildTool.Test](../ReBuildTool/ReBuildTool.Test) 中的 NUnit 测试内部都用到了它：

```csharp
project.Parse();   // 发现并编译 *.target.cs / *.module.cs
project.Setup();   // 按需生成默认脚手架 + 生成 IDE 工程
project.Build(targetName);
project.Clean();
project.ReBuild(targetName);
```

## 6. 旧版 INI 工程格式

除了 C# 规则以外，RBT 目前仍会解析一种基于 INI 的工程/模块格式
（`ReBuildTool.Ini`），并且每次运行时都会与 C# 工程并行处理。
自动生成的默认 `.target` 风格文件类似这样：

```ini
[Target]
+Entries="Runtime"

[Init]
+DependOn="Action:DoSomething"
+Actions=(Name="ReMake.Init", Args=(projectName="Sample"))

[Build]
# build actions

[Action:DoSomething]
+Actions=...
```

对应的 `.module.ini` 文件则包含一个 `[Module]` 段和
`+Dependencies=` 配置。这种格式早于 C# 规则系统出现——除非你确实需要
INI 里基于 `Bullseye` 实现的 action/target 依赖图功能，
否则新项目建议优先使用 `.target.cs` / `.module.cs`。

## 7. 自我更新

`ReBuildTool.Updater`（通过 `rbt-updater.sh` / `rbt-updater.bat` 调用）会拉取
`$RBT_HOME` 下最新的 `ReBuildTool` git 仓库，并从源码重新构建
（`BuildAll` 加上它自身），也就是说 RBT 会用自己的仓库来自举/更新自己：

```bash
./BuildScript/rbt-updater.sh
```

## 8. 快速参考

```bash
# 在空项目文件夹中做一次性初始化
./RBTBooster.sh --init MyGame

# 修改 Target/Module 规则或源码后重新构建
./BuildProject.sh
# 等价于：
./RBTBooster.sh --build MyGame

# 直接调用，获得更多控制权
ReBuildTool --ProjectRoot . --Mode Build --Target MyGame \
    --BuildConfig Release --TargetPlatform Windows --TargetArch x64
```
