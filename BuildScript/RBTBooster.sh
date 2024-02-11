#!/bin/bash

# Support by ReBuildTool ! auto update by ReBuildTool.
# Dot modify manually !!

CURRENT_MODE=$1
CURRENT_SCRIPT_FULL_PATH=$(realpath $0)
CURRENT_DIR=$(cd $(dirname $0); pwd)
echo "current dir is $CURRENT_DIR"

CURRENT_HOME=$HOME

if [ -z "$RBT_HOME" ]; then
    echo "RBT_HOME is not set"
    RBT_HOME=$CURRENT_HOME/.rbt
fi

echo "RBT_HOME is $RBT_HOME"

initReBuildTool()
{
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
            git pull
            git submodule update
            cd BuildScript
        fi

        chmod +x BuildAll.sh

        echo "============= Build ReBuildTool ================"
        ./BuildAll.sh
    else
      cd ReBuildTool/BuildScript
    fi

    echo "============= Run ReBuildTool ================"
    echo "input project name?"
    read TARGET_NAME

    if [ ! -z $TARGET_NAME ]; then
        echo "init $TARGET_NAME"
        cd ../Binary/MacArm64/ReBuildTool
        chmod +x ReBuildTool
        ./ReBuildTool --ProjectRoot $CURRENT_DIR --Mode Init --Target $TARGET_NAME --BoosterSource $CURRENT_SCRIPT_FULL_PATH
    else
        echo "no target name skip init"
    fi


}

main() {
   

    if [ "$CURRENT_MODE" = "--init" ]; then
        initReBuildTool
        exit 0
    else
        echo "invalid arg $CURRENT_MODE"
        echo "selection:"
        echo " --init : initialize RBT & setup current directory"
        exit 0
    fi
}

main

