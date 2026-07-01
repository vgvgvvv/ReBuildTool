# ReBuildTool (WIP)
yet another build system

## Installation

> **Prerequisites:** [Git](https://git-scm.com/) and [.NET 8 SDK](https://dotnet.microsoft.com/download) must be installed.

### Linux / macOS

```bash
curl -sSL https://raw.githubusercontent.com/vgvgvvv/ReBuildTool/main/BuildScript/Install.sh | bash
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

## Get Start
[Whats ReBuildTool](Doc/Whats-ReBuildTool.md)
