set CURRENT_SCRIPT_FULL_PATH=%0
set CURRENT_DIR=%~dp0
echo "current dir is %CURRENT_DIR%"
set CURRENT_HOME=%UserProfile%

if not defined RBT_HOME (
    echo "RBT_HOME is not set, use default"
    set RBT_HOME=%CURRENT_HOME%\.rbt
)

if not exist "%RBT_HOME%\" (
    set REBUILD_REBUILDTOOL="Y"
    mkdir %RBT_HOME%
) else (
    set REBUILD_REBUILDTOOL="N"
)

cd /d %RBT_HOME%
echo "goto %RBT_HOME% current dir is %cd%"

cd "ReBuildTool/Binary/Win64/ReBuildTool/"

call ReBuildTool.exe %*