dotnet clean
dotnet publish -c Release -f net5.0 -r linux-x64 --self-contained true

svn update ../../ExternalServerBin

cp -r bin/Release/net5.0/linux-x64/publish/* ../../ExternalServerBin/Chat

svn add --force ../../ExternalServerBin
svn cleanup ../../ExternalServerBin
svn commit ../../ExternalServerBin -m "update chat server bin"
