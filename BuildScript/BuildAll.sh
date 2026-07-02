#!/usr/bin/env bash

set -Eeuo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")"; pwd)"
cd "$SCRIPT_DIR"

RBT_HOME="${RBT_HOME:-"$HOME/.rbt"}"
export RBT_HOME

ProjectName=ReBuildTool
dotnet publish "../$ProjectName/$ProjectName/$ProjectName.csproj" -r win-x64 --self-contained -o "../Binary/Win64/$ProjectName"
dotnet publish "../$ProjectName/$ProjectName/$ProjectName.csproj" -r osx-x64 --self-contained -o "../Binary/Mac64/$ProjectName"
dotnet publish "../$ProjectName/$ProjectName/$ProjectName.csproj" -r osx-arm64 --self-contained -o "../Binary/MacArm64/$ProjectName"
dotnet publish "../$ProjectName/$ProjectName/$ProjectName.csproj" -r linux-x64 --self-contained -o "../Binary/Linux64/$ProjectName"

cp rbt.sh "$RBT_HOME/rbt.sh"
cp rbt-updater.sh "$RBT_HOME/rbt-updater.sh"
chmod +x "$RBT_HOME/rbt.sh"
chmod +x "$RBT_HOME/rbt-updater.sh"

cp rbt.sh "$RBT_HOME/rbt"
cp rbt-updater.sh "$RBT_HOME/rbt-updater"
chmod +x "$RBT_HOME/rbt"
chmod +x "$RBT_HOME/rbt-updater"
