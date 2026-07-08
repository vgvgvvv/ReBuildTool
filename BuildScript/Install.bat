 
set CURRENT_SCRIPT_FULL_PATH=%0
set CURRENT_DIR=%~dp0
echo "current dir is %CURRENT_DIR%"

set CURRENT_HOME=%UserProfile%

if not defined RBT_HOME (
    echo "RBT_HOME is not set, use default"
    set RBT_HOME=%CURRENT_HOME%\.rbt
)

echo "RBT_HOME is %RBT_HOME%"


if not exist "%RBT_HOME%\" (
    set REBUILD_REBUILDTOOL="Y"
    mkdir %RBT_HOME%
) else (
    echo "need rebuild ReBuildTool? [Y/N]"
    set /p REBUILD_REBUILDTOOL=
)

cd /d %RBT_HOME%
echo "goto %RBT_HOME% current dir is %cd%"

if "%REBUILD_REBUILDTOOL%"=="Y" (
    
    echo "============= Get ReBuildTool From Git ================"

    if not exist "%RBT_HOME%\ReBuildTool\" (
        echo "clone ReBuildTool in %cd%"
        git clone https://github.com/vgvgvvv/ReBuildTool.git
        cd ReBuildTool
        git submodule init
        git submodule update
        cd BuildScript
    ) else (
        echo "pull ReBuildTool"
        cd ReBuildTool
        git reset --hard
        git remote set-url origin https://github.com/vgvgvvv/ReBuildTool.git
        git pull
        git submodule sync --recursive
        git submodule update
        cd BuildScript
    )

    echo "============= Build ReBuildTool ================"

    call BuildAll.bat
    call BuildUpdater.bat
) else (
    cd "ReBuildTool/BuildScript"
)

echo ============= Add RBT to PATH =================
powershell -Command "$rbtHome = '%RBT_HOME%'; $userPath = [Environment]::GetEnvironmentVariable('Path', 'User'); if ($userPath -notlike ('*' + $rbtHome + '*')) { [Environment]::SetEnvironmentVariable('Path', $userPath + ';' + $rbtHome, 'User'); Write-Host 'Added RBT to PATH' } else { Write-Host 'RBT already in PATH' }"

echo ============= Installation Complete =================
echo Please restart your terminal to use the 'rbt' command.
