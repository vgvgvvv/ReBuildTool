#!/bin/bash


initReBuildTool()
{
    CURRENT_DIR=$(cd $(dirname $0); pwd)
    echo "current dir is $CURRENT_DIR"

    mkdir -p Intermedia/RBT/
    cd Intermedia/RBT/

    echo "need rebuild ReBuildTool?[Y/N]"
    read REBUILD_REBUILDTOOL

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
    fi

    echo "============= Run ReBuildTool ================"
    echo "input project name?"
    read TARGET_NAME
    
    cd ../Binary/MacArm64/ReBuildTool
    chmod +x ReBuildTool
    ./ReBuildTool --ProjectRoot $CURRENT_DIR --Mode Init --Target $TARGET_NAME
}

if [ "$1" = "--init" ]; then
    initReBuildTool
else
    exit 1
fi