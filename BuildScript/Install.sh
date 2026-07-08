#!/usr/bin/env bash

set -Eeuo pipefail

REPOSITORY_URL="https://github.com/vgvgvvv/ReBuildTool.git"
RBT_HOME="${RBT_HOME:-"$HOME/.rbt"}"
REPOSITORY_DIR="$RBT_HOME/ReBuildTool"
export RBT_HOME

assume_yes=false
for argument in "$@"; do
    case "$argument" in
        -y|--yes)
            assume_yes=true
            ;;
        *)
            echo "Unknown argument: $argument" >&2
            echo "Usage: bash Install.sh [-y|--yes]" >&2
            exit 2
            ;;
    esac
done

echo "RBT_HOME is $RBT_HOME"
mkdir -p "$RBT_HOME"

rebuild=true
if [ -d "$REPOSITORY_DIR/.git" ]; then
    if [ "$assume_yes" = true ]; then
        rebuild=true
    elif [ -t 1 ]; then
        response=""
        if read -r -p "ReBuildTool is already installed. Update and rebuild it? [y/N] " response </dev/tty; then
            case "$response" in
                y|Y|yes|YES|Yes)
                    rebuild=true
                    ;;
                *)
                    rebuild=false
                    ;;
            esac
        else
            echo
            echo "Could not read from the terminal; skipping rebuild."
            rebuild=false
        fi
    else
        echo "No interactive terminal detected; updating the existing installation."
        rebuild=true
    fi
elif [ -e "$REPOSITORY_DIR" ]; then
    echo "Cannot install: $REPOSITORY_DIR exists but is not a Git repository." >&2
    exit 1
fi

if [ "$rebuild" = true ]; then
    echo "============= Get ReBuildTool From Git ================"
    if [ ! -d "$REPOSITORY_DIR/.git" ]; then
        git clone "$REPOSITORY_URL" "$REPOSITORY_DIR"
    else
        git -C "$REPOSITORY_DIR" remote set-url origin "$REPOSITORY_URL"
        git -C "$REPOSITORY_DIR" pull --ff-only
    fi

    git -C "$REPOSITORY_DIR" submodule sync --recursive
    git -C "$REPOSITORY_DIR" submodule update --init --recursive

    echo "============= Build ReBuildTool ================"
    "$REPOSITORY_DIR/BuildScript/BuildAll.sh"
    "$REPOSITORY_DIR/BuildScript/BuildUpdater.sh"
fi

echo "============= Add RBT to PATH ================"
export_line="export PATH=\"\$PATH:$RBT_HOME\""
shell_configs=("$HOME/.bashrc" "$HOME/.zshrc" "$HOME/.profile")
config_found=false

for config in "${shell_configs[@]}"; do
    if [ -f "$config" ]; then
        config_found=true
        if ! grep -qF "$RBT_HOME" "$config"; then
            printf '\n# ReBuildTool\n%s\n' "$export_line" >>"$config"
            echo "Added RBT to PATH in $config"
        else
            echo "RBT already in PATH in $config"
        fi
    fi
done

if [ "$config_found" = false ]; then
    case "${SHELL:-}" in
        */zsh)
            config="$HOME/.zshrc"
            ;;
        *)
            config="$HOME/.profile"
            ;;
    esac
    printf '# ReBuildTool\n%s\n' "$export_line" >>"$config"
    echo "Added RBT to PATH in $config"
fi

echo "============= Installation Complete ================"
echo "Please restart your terminal or add RBT to the current session with:"
echo "  export PATH=\"\$PATH:$RBT_HOME\""
echo "Then you can use the 'rbt' command."
