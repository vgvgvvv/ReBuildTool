cd $(dirname $0)

CURRENT_HOME=$HOME

if [ -z "$RBT_HOME" ]; then
    echo "RBT_HOME is not set"
    RBT_HOME=$CURRENT_HOME/.rbt
fi


ProjectName=ReBuildTool
dotnet publish ../$ProjectName/$ProjectName/$ProjectName.csproj -r win-x64 --self-contained -o ../Binary/Win64/$ProjectName
dotnet publish ../$ProjectName/$ProjectName/$ProjectName.csproj -r osx-x64 --self-contained -o ../Binary/Mac64/$ProjectName
dotnet publish ../$ProjectName/$ProjectName/$ProjectName.csproj -r osx-arm64 --self-contained -o ../Binary/MacArm64/$ProjectName
dotnet publish ../$ProjectName/$ProjectName/$ProjectName.csproj -r linux-x64 --self-contained -o ../Binary/Linux64/$ProjectName

cp rbt.sh $RBT_HOME/rbt.sh
cp rbt-updater.sh $RBT_HOME/rbt-updater.sh
chmod +x $RBT_HOME/rbt.sh
chmod +x $RBT_HOME/rbt-updater.sh

cp rbt.sh $RBT_HOME/rbt
cp rbt-updater.sh $RBT_HOME/rbt-updater
chmod +x $RBT_HOME/rbt
chmod +x $RBT_HOME/rbt-updater