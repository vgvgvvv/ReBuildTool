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
| `--Mode <RunMode>` | **必填。** 取值为 `Init`、`Build`、`Clean`、`ReBuild`、`Restore` 之一。 |
| `--Target <name>` | 要构建的目标名称，默认为 `ProjectRoot` 文件夹名。 |
| `--IDEProjectType <type>` | `Init` 模式下生成哪种工程：`VisualStudio`、`CMake`、`VSCode` 或 `CompileCommands`。默认 Windows 为 Visual Studio，其他平台为 CMake。 |
| `--BoosterSource <path>` | 内部参数，由 Booster 脚本设置，用于 RBT 重新生成这些脚本。请勿手动设置。 |

`Mode` 的行为（详见
[Program.cs](../ReBuildTool/ReBuildTool/Program.cs)）：
- **Init** —— 如果 `Source/` 下还没有任何 Target/Module，则生成默认的一份，
  然后生成 IDE 工程（Visual Studio 的 `.sln`，或 CMake 工程）。无论生成哪种工程，
  都会在工程根目录写出 `compile_commands.json`（JSON 编译数据库，供 clangd / VS Code /
  CLion 实现代码高亮与跳转），也可通过 `--IDEProjectType CompileCommands` 单独生成。
  使用 `--IDEProjectType VSCode` 时，会在工程根目录写出 `.vscode/` 文件夹
  （`tasks.json`、`launch.json`、`c_cpp_properties.json`），让 VS Code 能够构建、
  运行、调试与清理工程 —— 所有操作都会委托回 RBT 执行，并为每个可执行模块生成
  一个运行任务和一个调试启动配置。生成出来的任务会带上生成工程时使用的平台参数
  （`--TargetPlatform`、`--TargetArch` 等），因此用 `--TargetPlatform Android` 生成的工程
  在 VS Code 中构建时同样会针对 Android 编译，而不会退回到宿主平台。
- **Build** —— 编译指定的目标。
- **Clean** —— 清理构建产物。
- **ReBuild** —— 先 `Clean` 再 `Build`。
- **Restore** —— 拉取 `RBTPackage.json` 里声明的包并写出 lock 文件，然后停止。
  其它模式都会先隐式执行一次 restore，所以这个模式只用于提前把依赖准备好
  （预热 CI 缓存，或者趁机器还有网络时先把依赖拉下来）。
  见 [§5 包管理](#5-包管理)。

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
- ~~`List<GitLibrary> GitLibraries`~~ —— **已废弃，且从未被读取。** 请改用
  `RBTPackage.json` 声明依赖（[§5](#5-包管理)）。它待在这个位置上就不可能工作：
  该列表挂在 target rule 上，而 target rule 只有在规则程序集编译完成后才存在，
  但包自带的 `.module.cs` 必须在那次编译**之前**就落到磁盘上。
- `List<ITargetCompilePlugin> Plugins` —— 编译前/后钩子。
- `virtual void Setup(ICppBuildContext)` / `virtual void PostBuild()` —— 可重写以实现自定义逻辑。

### Module 规则（`*.module.cs`）

```csharp
using ReBuildTool.ToolChain;

public class MyGameModule : CppModuleRule
{
    public override void Setup(ICppBuildContext buildContext)
    {
        TargetBuildType = BuildType.StaticLibrary; // 或 DynamicLibrary / Executable
        Dependencies.Add("SomeOtherModule");
        PublicIncludePaths.Add("Public");
    }
}
```

> **声明写在 `Setup` 里，不要写在构造函数里。** 模块规则的这些列表会在每一轮
> setup 时重建——build context 变化时（IDE 工程生成和构建各自持有一个）同一个规则
> 对象会被重新 setup，`Cleanup` 会先清空列表以避免重复追加。构造函数里写的东西在第
> 二轮就没了，因此 rbt 会直接拒绝在构造函数里声明的规则，并在报错里列出是哪几个属
> 性。这么做换来的是 `buildContext`：`Setup` 里可以按当前构建的平台、架构、配置分支
> 处理。

Target 规则不同：它的 `UsedModules` / `Plugins` 在任何 target `Setup` 执行之前就会
被读取，所以这些仍然写在构造函数里。

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

## 5. 包管理

项目在 `Source/` 旁边的 `RBTPackage.json` 里声明外部依赖。每次构建前 RBT 会拉取
缺失的包，把它们物化到 `Packages/` 下，并把实际解析到的结果记录进
`RBTPackage.lock.json`。

```jsonc
{
  "name": "MyGame",
  "dependencies": {
    // git 包，固定到某个 tag
    "GreeterLib": { "git": "https://github.com/x/greeter.git", "tag": "v1.2.0" },
    // 固定到精确 commit
    "FooLib":     { "git": "https://github.com/x/foo.git", "commit": "a1b2c3d4..." },
    // 发布压缩包，按哈希校验
    "zlib":       { "url": "https://.../zlib-1.3.tar.gz", "sha256": "…", "strip": 1 },
    // 本机上的目录，用于本地联调
    "LocalLib":   { "path": "../LocalLib" }
  }
}
```

每条依赖**有且只有一个**来源（`git`、`url` 或 `path`）；git 来源必须带上 `commit`、
`tag` 或 `branch` —— RBT 只接受精确 pin，永远不会替你挑版本。

`url` 支持 `.zip`、`.tar.gz`/`.tgz` 和 `.tar`。`strip` 会丢掉指定数量的前导路径段，
等同于 `tar --strip-components` —— 因为发布用的 tarball 基本都会把内容包在一层
`name-version/` 目录里。URL 不像 commit 那样自带校验能力：它背后的字节可以在清单不变的
情况下被换掉，所以请给它写上 `sha256`；不匹配会中止 restore 并打印两个哈希。

### 包的三种形态

**1. 源码包**自带 `.module.cs`。没有任何特殊处理：它的规则会和项目自己的规则一起被
glob 进同一个 `CompileRules.dll`，因此包里的模块和本地模块一样按名字依赖：

```csharp
Dependencies.Add("GeometryModule");
```

包提供的是**模块，而不是 target** —— 构建什么始终由消费方项目决定，所以包里的
`*.target.cs` 会被忽略。包名和它里面的模块名互相独立；参见
[Sample/PackageConsumer](../Sample/PackageConsumer) 及它消费的
[Sample/GeometryPackage](../Sample/GeometryPackage)。

**2. 预编译二进制包**只带头文件和库，不带规则。它在自己的清单里声明产物，RBT 负责合成模块：

```jsonc
{
  "name": "SomePrebuilt",
  "binary": {
    "module": "SomePrebuiltModule",          // 缺省为包名
    "includes": ["include"],
    "artifacts": [
      { "platform": "Windows", "arch": "x64", "config": "Release",
        "libraryDirectories": ["lib/win-x64"], "staticLibraries": ["some.lib"] },
      { "platform": "Linux", "arch": "x64",
        "libraryDirectories": ["lib/linux-x64"], "staticLibraries": ["libsome.a"] }
    ]
  }
}
```

`platform` 对应 `--TargetPlatform`，`arch` 对应 `--TargetArch`，`config` 对应
`--BuildConfig`；**省略某一项即表示匹配全部取值**。产物是在构建 setup 阶段选择的，
不是在生成规则文件时选的，因此切换目标平台不会让任何缓存失效。若一条都匹配不上，
RBT 会给出警告，而不是悄悄链接一个空的。

**3. 第三方原样源码**既没有头文件+库，也没有规则 —— 就是别人的 `src/` 目录结构。
由消费方项目通过 `overlay` 提供规则：

```jsonc
"glfw": {
  "git": "https://github.com/glfw/glfw.git", "tag": "3.4",
  "overlay": "Overlays/glfw.module.cs"
}
```

overlay 会被复制进包内，这样它里面相对路径形式的 `SourceDirectories`、`SourceFiles`、
`ExcludeDirectories`、`ExcludeFiles` 才能正确解析到上游代码树 —— 这几个成员本来就是
为这类库准备的。

### 传递依赖

包在自己的 `RBTPackage.json` 里声明自己的依赖，RBT 会沿着图往下走：拉一个包，读它
带来的清单，再拉清单里点名的包，如此往复。被多条路径引用到的包只会拉取一次。

因为没有版本求解器，两个包对同一个依赖给出不同的 pin 属于**硬错误**。请在根清单里
显式指定哪个胜出：

```jsonc
{ "overrides": { "FooLib": { "git": "https://github.com/x/foo.git", "tag": "v2.0" } } }
```

依赖成环会被拒绝，并把整条链路打印出来。

### lock 文件

`RBTPackage.lock.json` 记录每个 pin 实际解析到的 commit —— tag 可能被上游移动，
commit 不会 —— 这样后续 restore 能复现同一棵树，且完全不需要网络。**请提交它。**
它只在内容真正变化时才重写，不会污染工作区。

### 相关参数

| 参数 | 作用 |
|---|---|
| `--Offline` | 绝不访问网络。若 lock 尚未在磁盘上被满足则直接失败。 |
| `--ForceRestore` | 即使 lock 已满足也重新拉取所有包。 |
| `--UpdateLock` | 重新解析会移动的 pin（tag / branch）并重写 lock，相当于 `cargo update`。 |

### 东西放在哪

`Packages/` 位于项目根目录，**不在** `Intermedia/` 下：`Clean` 会清空
`Intermedia/`，而且只要 RBT 的二进制比上次构建新，它就会自行触发一次 clean ——
放在那里意味着每次 rebuild、每次 RBT 升级都要重新下载全部依赖。restore 会把
`/Packages/` 加进项目的 `.gitignore`；`path` 依赖在原地使用，不会被复制。

没有 `RBTPackage.json` 的项目完全不受影响 —— 不会有 `Packages/` 目录、不会有 lock
文件、也不会改动 `.gitignore`。

## 6. 编程式 / 生命周期 API

所有工程类型都暴露相同的生命周期方法，`Program.cs` 中的模式分发逻辑，以及
[ReBuildTool.Test](../ReBuildTool/ReBuildTool.Test) 中的 NUnit 测试内部都用到了它：

```csharp
project.Parse();   // 发现并编译 *.target.cs / *.module.cs
project.Setup();   // 按需生成默认脚手架 + 生成 IDE 工程
project.Build(targetName);
project.Clean();
project.ReBuild(targetName);
```

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
