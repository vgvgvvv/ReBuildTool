#!/bin/bash

CURRENT_DIR=$(cd $(dirname $0); pwd)

case "$(uname -s)" in
    Linux*)    OS="Linux";;
    Darwin*)   OS="Mac";;
    *)         OS="UNKNOWN: $(uname -s)"
esac

case "$(uname -m)" in
    x86_64)   ARCH="64";;
    arm64)    ARCH="Arm64";;
    *)        ARCH="UNKNOWN: $(uname -m)"
esac


"$CURRENT_DIR/ReBuildTool/Binary/$OS$ARCH/ReBuildTool.Updater/ReBuildTool.Updater" %*