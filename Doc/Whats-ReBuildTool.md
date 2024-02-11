# Whats ReBuildTool

yet another build tool just for myself.

## How to start
copy the RBTBooster.sh/.bat to a empty folder, and then run in shell

```
./RBTBooster --init
```

then everything will be done itself.

## Folder structure
just like unreal, RBT project contains targets and modules.

Targets has 'Entries' which are modules. 

And module can depend on other modules

Both of them must have Init and Build sections for run, and serials of some action.


``` Ini
# Sample.target.ini

[Target]
+Entries="Runtime"

[Init]
# init actions
# Depend on other action
+DependOn="Action:DoSomething"
+Actions=(Name="ReMake.Init", Args=(projectName="Sample"))

[Build]
# build actions

[Action:DoSomething]
+Actions=...
```

```Ini
# Runtime.module.ini

[Module]
+Dependencies="ReClass"

[Init]
# init actions
+Actions=(Name="ReMake.Init", Args=(projectName="Sample"))

[Build]
# build actions
```