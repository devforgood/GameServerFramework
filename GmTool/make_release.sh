dotnet clean
dotnet publish -c Release -f netcoreapp3.1 -r linux-x64 --self-contained true

cp -r bin/Release/netcoreapp3.1/linux-x64/publish/* ../../../DevFiles/ServerFiles/GMTool

