cd  %~dp0

set CURRENT_HOME=%UserProfile%

if not defined RBT_HOME (
    echo "RBT_HOME is not set, use default"
    set RBT_HOME=%CURRENT_HOME%\.rbt
)

set ProjectName=ReBuildTool
dotnet publish ..\%ProjectName%\%ProjectName%\%ProjectName%.csproj -r win-x64 -o ..\Binary\Win64\%ProjectName%
dotnet publish ..\%ProjectName%\%ProjectName%\%ProjectName%.csproj -r osx-x64 -o ..\Binary\Mac64\%ProjectName%
dotnet publish ..\%ProjectName%\%ProjectName%\%ProjectName%.csproj -r osx-arm64 -o ..\Binary\MacArm64\%ProjectName%
dotnet publish ..\%ProjectName%\%ProjectName%\%ProjectName%.csproj -r linux-x64 -o ..\Binary\Linux64\%ProjectName%


xcopy /Y /f "rbt.bat" "%RBT_HOME%\rbt.bat*"
xcopy /Y /f "rbt-updater.bat" "%RBT_HOME%\rbt-updater.bat*" 