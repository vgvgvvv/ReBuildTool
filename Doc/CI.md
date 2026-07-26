# Continuous Integration

[中文版](CI.zh-CN.md)

Every push and every pull request runs [`.github/workflows/ci.yml`](../.github/workflows/ci.yml).
Tags are excluded — those are handled by `release.yml`.

## Jobs

| Job | Runner | What it covers |
| --- | --- | --- |
| `Test (Windows x64)` | `windows-2025` | Builds the solution and runs the NUnit suite against the MSVC toolchain |
| `Test (Linux x64)` | `ubuntu-latest` | Same suite against GCC |
| `Test (macOS arm64)` | `macos-latest` | Same suite against the XCode clang toolchain |
| `Publish (<rid>)` | `ubuntu-latest` | Self-contained publish of `ReBuildTool` + `ReBuildTool.Updater` for `win-x64`, `osx-x64`, `osx-arm64`, `linux-x64` |
| `CI` | `ubuntu-latest` | Aggregate gate — green only when every job above passed |

The test suite drives real C/C++ builds of the projects under `Sample/`, so each
platform leg exercises that platform's toolchain end to end: source discovery,
compile, archive/link, IDE project generation and HeaderTool codegen. Test
results (`.trx`) are uploaded as an artifact from every leg, and the published
binaries are kept for 7 days.

Tests that only apply to one platform call `Assert.Ignore` rather than failing —
the Visual Studio project tests off Windows, and the VS setup-API test.

## Requiring CI on every commit

The `CI` job exists to be the single required status check. To enforce it:

**Settings → Branches → Add branch ruleset** for `main`, enable
*Require status checks to pass*, and select **CI**. With
*Require branches to be up to date* on as well, a commit can only land on `main`
after the whole matrix has passed against the current tip.

## Notes on the environment

- **Submodules** are checked out recursively (`Vendor/ReCSharpCommon`,
  `Vendor/UniToLua`); the solution does not build without them.
- **ResetHeaderTool is provisioned by the workflow.** rbt normally bootstraps it
  by cloning over SSH and running its build script, which cannot work on a runner
  (no key, and the upstream repo has no `Scripts/BuildAll.sh`). The workflow
  clones it over HTTPS into `Sample/HeaderToolTest/Intermedia/ResetHeaderTool` and
  publishes the host binary itself; rbt then skips its own bootstrap because the
  clone is already there.
- **`InterMedia` symlink on Linux.** ResetHeaderTool reads the project info rbt
  writes under `Intermedia/` from a path it spells `InterMedia/`. Same directory
  on Windows and macOS, missing on a case-sensitive filesystem, so the workflow
  creates the alias. It can be dropped once the casing is fixed upstream.
- **The Windows runner is pinned** to `windows-2025` instead of `windows-latest`:
  the native toolchain under test is whatever Visual Studio the image ships, so
  it is chosen deliberately rather than moving when GitHub re-points the label.
- **NuGet packages are cached** per runner OS, keyed on the `.csproj` files.

## Running the same checks locally

```bash
git submodule update --init --recursive
dotnet restore ReBuildTool/ReBuildTool.sln
dotnet build   ReBuildTool/ReBuildTool.sln -c Release --no-restore
dotnet test    ReBuildTool/ReBuildTool.sln -c Release --no-build
```

`TestHeaderToolCodegen` additionally needs ResetHeaderTool. Either let rbt
bootstrap it (requires SSH access to `vgvgvvv/ResetHeaderTool`, Windows only) or
provision it the way the workflow does.
