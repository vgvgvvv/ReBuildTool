# 持续集成

[English](CI.md)

每次 push 和每个 pull request 都会运行 [`.github/workflows/ci.yml`](../.github/workflows/ci.yml)。
tag 不在此列——那由 `release.yml` 负责。

## Job 构成

| Job | Runner | 覆盖内容 |
| --- | --- | --- |
| `Test (Windows x64)` | `windows-2025` | 编译解决方案并在 MSVC 工具链上跑 NUnit 测试 |
| `Test (Linux x64)` | `ubuntu-latest` | 同一套测试，走 GCC |
| `Test (macOS arm64)` | `macos-latest` | 同一套测试，走 XCode clang |
| `Publish (<rid>)` | `ubuntu-latest` | 为 `win-x64`、`osx-x64`、`osx-arm64`、`linux-x64` 自包含发布 `ReBuildTool` 与 `ReBuildTool.Updater` |
| `CI` | `ubuntu-latest` | 汇总门禁——以上全部通过才为绿 |

测试会真正编译 `Sample/` 下的 C/C++ 工程，因此每条平台分支都完整走一遍该平台的
工具链：源文件搜集、编译、打包/链接、IDE 工程生成以及 HeaderTool 代码生成。
每条分支都会上传测试结果（`.trx`）作为 artifact，发布产物保留 7 天。

只适用于单一平台的测试用 `Assert.Ignore` 跳过而不是失败——非 Windows 上的 Visual
Studio 工程生成测试，以及 VS setup API 测试。

## 让每次提交都必须通过

`CI` 这个 job 的存在意义就是作为唯一的必需状态检查。启用方式：

**Settings → Branches → Add branch ruleset**，针对 `main` 打开
*Require status checks to pass*，勾选 **CI**。同时打开
*Require branches to be up to date*，则只有整个矩阵在最新 tip 上跑通后，提交才能进
`main`。

## 环境说明

- **递归检出 submodule**（`Vendor/ReCSharpCommon`、`Vendor/UniToLua`），否则解决方案
  无法编译。
- **ResetHeaderTool 由 workflow 预置。** rbt 原本通过 SSH clone 并执行其构建脚本来
  引导，这在 runner 上行不通（没有密钥，且上游仓库没有 `Scripts/BuildAll.sh`）。
  workflow 改为用 HTTPS clone 到
  `Sample/HeaderToolTest/Intermedia/ResetHeaderTool` 并自行发布宿主平台的二进制；
  由于 clone 已存在，rbt 会跳过自己的引导流程。
- **Linux 上的 `InterMedia` 符号链接。** rbt 把工程信息写在 `Intermedia/` 下，而
  ResetHeaderTool 读取时拼的是 `InterMedia/`。在 Windows 与 macOS 上是同一个目录，
  在大小写敏感的文件系统上则不存在，所以 workflow 建了这个别名。上游修正大小写后
  即可删除。
- **Windows runner 固定为** `windows-2025` 而非 `windows-latest`：被测的原生工具链
  就是镜像自带的那个 Visual Studio，与其让 GitHub 重新指向标签时跟着变，不如显式
  选定。
- **NuGet 包按 runner 系统缓存**，key 取自各 `.csproj`。

## 在本地跑同样的检查

```bash
git submodule update --init --recursive
dotnet restore ReBuildTool/ReBuildTool.sln
dotnet build   ReBuildTool/ReBuildTool.sln -c Release --no-restore
dotnet test    ReBuildTool/ReBuildTool.sln -c Release --no-build
```

`TestHeaderToolCodegen` 还需要 ResetHeaderTool：要么让 rbt 自行引导（需要
`vgvgvvv/ResetHeaderTool` 的 SSH 访问权限，且仅限 Windows），要么照 workflow 的做法
预置。
