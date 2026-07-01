CURRENT_MODE=$1
TARGET_NAME=$2
CURRENT_SCRIPT_FULL_PATH=$(realpath $0)
CURRENT_DIR=$(cd $(dirname $0); pwd)
echo "current dir is $CURRENT_DIR"

CURRENT_HOME=$HOME

if [ -z "$RBT_HOME" ]; then
    echo "RBT_HOME is not set"
    RBT_HOME=$CURRENT_HOME/.rbt
fi

echo "RBT_HOME is $RBT_HOME"

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

if [ ! -d $RBT_HOME ]; then
    REBUILD_REBUILDTOOL="Y"
    mkdir -p $RBT_HOME
else
    echo "need rebuild ReBuildTool?[Y/N]"
    read REBUILD_REBUILDTOOL
fi
cd $RBT_HOME

if [ $REBUILD_REBUILDTOOL == "Y" ]; then

    echo "============= Get ReBuildTool From Git ================"
    if [ ! -d "ReBuildTool" ];
    then
        echo "clone ReBuildTool"
        git clone git@github.com:vgvgvvv/ReBuildTool.git
        cd ReBuildTool
        git submodule init
        git submodule update
        cd BuildScript/
    else
        echo "pull ReBuildTool"
        cd ReBuildTool
        git reset --hard
        git pull
        git submodule update
        cd BuildScript
    fi

    chmod +x BuildAll.sh
    chmod +x BuildUpdater.sh

    echo "============= Build ReBuildTool ================"
    ./BuildAll.sh
    ./BuildUpdater.sh
else
    cd ReBuildTool/BuildScript
fi

echo "============= Add RBT to PATH ================"
EXPORT_LINE="export PATH=\"\$PATH:$RBT_HOME\""
SHELL_CONFIGS=("$HOME/.bashrc" "$HOME/.zshrc" "$HOME/.profile")
for CONFIG in "${SHELL_CONFIGS[@]}"; do
    if [ -f "$CONFIG" ]; then
        if ! grep -qF "$RBT_HOME" "$CONFIG"; then
            printf '\n# ReBuildTool\n%s\n' "$EXPORT_LINE" >> "$CONFIG"
            echo "Added RBT to PATH in $CONFIG"
        else
            echo "RBT already in PATH in $CONFIG"
        fi
    fi
done

echo "============= Installation Complete ================"
echo "Please restart your terminal or run:"
echo "  source ~/.bashrc   (bash)"
echo "  source ~/.zshrc    (zsh)"
echo "Then you can use the 'rbt' command."