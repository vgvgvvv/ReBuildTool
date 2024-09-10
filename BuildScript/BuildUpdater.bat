cd  %~dp0

set ProjectName=ReBuildTool.Updater
set SlnName=ReBuildTool
dotnet publish ..\%SlnName%\%ProjectName%\%ProjectName%.csproj -r win-x64 --self-contained -o ..\Binary\Win64\%ProjectName%
dotnet publish ..\%SlnName%\%ProjectName%\%ProjectName%.csproj -r osx-x64 --self-contained -o ..\Binary\Mac64\%ProjectName%
dotnet publish ..\%SlnName%\%ProjectName%\%ProjectName%.csproj -r osx-arm64 --self-contained -o ..\Binary\MacArm64\%ProjectName%
dotnet publish ..\%SlnName%\%ProjectName%\%ProjectName%.csproj -r linux-x64 --self-contained -o ..\Binary\Linux64\%ProjectName%
