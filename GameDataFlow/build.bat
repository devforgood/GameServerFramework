protoc --proto_path=.  --python_out=.  --cpp_out=../GameDataProtobuf  --csharp_out=../Client/Assets/Scripts/GameData  gamedata.proto
if errorlevel 1 exit /b %errorlevel%

python GameDataFlow.py
