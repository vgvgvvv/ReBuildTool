cd $(dirname $0)

ProjectName=ReBuildTool.Updater
SlnName=ReBuildTool
dotnet publish ../$ReBuildTool/$ProjectName/$ProjectName.csproj -r win-x64 -o ../Binary/Win64/$ProjectName
dotnet publish ../$ReBuildTool/$ProjectName/$ProjectName.csproj -r osx-x64 -o ../Binary/Mac64/$ProjectName
dotnet publish ../$ReBuildTool/$ProjectName/$ProjectName.csproj -r osx-arm64 -o ../Binary/MacArm64/$ProjectName
dotnet publish ../$ReBuildTool/$ProjectName/$ProjectName.csproj -r linux-x64 -o ../Binary/Linux64/$ProjectName