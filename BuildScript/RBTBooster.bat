

rem @echo off
rem Support by ReBuildTool ! auto update by ReBuildTool.
rem Dot modify manually !!


set CURRENT_MODE=%1
set TARGET_NAME=%2
set CURRENT_SCRIPT_FULL_PATH=%0
set CURRENT_DIR=%~dp0
echo "current dir is %CURRENT_DIR%"

set CURRENT_HOME=%UserProfile%

if not defined RBT_HOME (
    echo "RBT_HOME is not set, use default"
    set RBT_HOME=%CURRENT_HOME%\.rbt
)

echo "RBT_HOME is %RBT_HOME%"

:initReBuildTool 
    if not exist %RBT_HOME%\ (
        set REBUILD_REBUILDTOOL="Y"
        mkdir %RBT_HOME%
    )
    else (
        echo "need rebuild ReBuildTool? [Y/N]"
        set /p REBUILD_REBUILDTOOL=
    )

    cd %RBT_HOME%

    if "%REBUILD_REBUILDTOOL%"=="Y" (
        
        echo "============= Get ReBuildTool From Git ================"

        if not exist %RBT_HOME%\ReBuildTool\ (
            echo "clone ReBuildTool"
            git clone git@github.com:vgvgvvv/ReBuildTool.git
            cd ReBuildTool
            git submodule init
            git submodule update
            cd BuildScript
        )
        else (
            echo "pull ReBuildTool"
            cd ReBuildTool
            git reset --hard
            git pull
            git submodule update
            cd BuildScript
        )

        echo "============= Build ReBuildTool ================"

        call BuildAll.bat
    )
    else (
        cd "ReBuildTool/BuildScript"
    )

    echo "============= Run ReBuildTool ================"
    if %TARGET_NAME% == "" (
        echo "input project name?"
        set /p TARGET_NAME=
    )

    if not %TARGET_NAME% == "" (
        echo "init %TARGET_NAME%"
        cd "../Binary/Win64/ReBuildTool"

        call ReBuildTool.exe --ProjectRoot %CURRENT_DIR% --Mode Init --Target %TARGET_NAME% --BoosterSource %CURRENT_SCRIPT_FULL_PATH%
    )
    else (
        echo "no target name skip init"
    )
exit /b 0

:buildProject
    echo "not implemented.."
    rem TODO: implement

    echo "============= Run ReBuildTool ================"
    if %TARGET_NAME% == "" (
        echo "input project name?"
        set /p TARGET_NAME=
    )

    call ReBuildTool.exe --ProjectRoot %CURRENT_DIR% --Mode Build --Target %TARGET_NAME% --BoosterSource %CURRENT_SCRIPT_FULL_PATH%

exit /b 0

:main
    if %CURRENT_MODE%=="init" (
        call :initReBuildTool
        exit /b 0
    )
    else if %CURRENT_MODE%=="build" (
        call :buildProject
        exit /b 0
    )
    else (
        echo "invalid arg %CURRENT_MODE%"
        echo "usage [mode] [targetName]"
        echo " --init : initialize RBT & setup current directory"
        exit /b 1
    )
exit /b 0

call :main