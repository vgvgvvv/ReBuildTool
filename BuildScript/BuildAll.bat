cd  %~dp0

set ProjectName=ReBuildTool
dotnet publish ..\%ProjectName%\%ProjectName%\%ProjectName%.csproj -r win-x64 -o ..\Binary\Win64\%ProjectName%
dotnet publish ..\%ProjectName%\%ProjectName%\%ProjectName%.csproj -r osx-x64 -o ..\Binary\Mac64\%ProjectName%
dotnet publish ..\%ProjectName%\%ProjectName%\%ProjectName%.csproj -r linux-x64 -o ..\Binary\Linux64\%ProjectName%

pause