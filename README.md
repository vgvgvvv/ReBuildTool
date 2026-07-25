# ReBuildTool (WIP)

A C#-driven native build system in the spirit of Unreal Engine's UBT. Describe **Targets** and **Modules** as small C# classes (`.target.cs` / `.module.cs`); RBT compiles those rule files on the fly and drives a real C/C++ toolchain (MSVC, Clang, GCC, Wasm) and IDE project generator (Visual Studio / CMake).

## Features

- **C# rule files** — `.target.cs` / `.module.cs` describe your build graph; no separate DSL to learn
- **Multi-toolchain** — MSVC (VS2017/2019/2022 auto-detect), Clang, GCC, Wasm
- **Cross-platform** — Windows, Linux, macOS (x64 & arm64)
- **IDE integration** — generates Visual Studio `.sln` and CMake projects
- **Self-updating** — `rbt-updater` rebuilds RBT from its own repo

## Installation

> **Prerequisites:** [Git](https://git-scm.com/) and [.NET 8 SDK](https://dotnet.microsoft.com/download) must be installed.

### Linux / macOS

```bash
curl -fsSL https://raw.githubusercontent.com/vgvgvvv/ReBuildTool/main/BuildScript/Install.sh | bash
```

After installation, restart your terminal or run `source ~/.bashrc` (or `~/.zshrc`), then verify:

```bash
rbt --help
```

### Windows (PowerShell)

```powershell
Invoke-WebRequest "https://raw.githubusercontent.com/vgvgvvv/ReBuildTool/main/BuildScript/Install.bat" -OutFile "$env:TEMP\rbt-install.bat"; cmd /c "$env:TEMP\rbt-install.bat"
```

After installation, restart your terminal, then verify:

```powershell
rbt --help
```

> The installer clones ReBuildTool to `~/.rbt` (Linux/macOS) or `%USERPROFILE%\.rbt` (Windows), builds all binaries, and adds the directory to your `PATH` automatically.

## Quick Start

```bash
# Bootstrap a new project
./RBTBooster.sh --init MyGame

# Build
./BuildProject.sh
# or directly:
rbt --ProjectRoot . --Mode Build --Target MyGame
```

## Documentation

- [What is ReBuildTool](Doc/Whats-ReBuildTool.md)
- [How To Use](Doc/HowToUse.md) | [中文使用指南](Doc/HowToUse.zh-CN.md)
