# Build Environment

This project is built and verified on Windows with Visual Studio 2022 Community.

## Server Build

Use the Visual Studio MSBuild executable, not `dotnet msbuild`, because the solution contains C++ projects that require `Microsoft.Cpp.Default.props`.

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\amd64\MSBuild.exe" GameServerFramework.sln /t:Game /p:Configuration=Debug /p:Platform=x64 /v:minimal
```

Verified output:

```text
x64\Debug\Game.exe
```

The first build may take longer than two minutes. If the sandbox blocks writes to C++ build state files under project `x64\Debug\*.tlog`, rerun the same MSBuild command with elevated permission.

## FlatBuffers Code Generation

The bundled compiler is:

```text
flatbuffer\flatc.exe
```

For `syncnet.fbs`, this old FlatBuffers 1.12 compiler may fail when writing directly to the target output directories. The reliable workflow is to generate into the `flatbuffer` directory, then copy the generated files:

There are **two** C++ copies of the generated header and both must be updated. `Engine`
compiles against `Engine\flatbuffers\syncnet_generated.h` (its include path), while `Game`
uses `Game\syncnet_generated.h`. Updating only one leaves the Engine building against a stale
protocol — it still compiles, so nothing tells you until the new messages silently do nothing.

```powershell
cd flatbuffer
.\flatc.exe --cpp -o . --gen-all syncnet.fbs
Copy-Item -LiteralPath .\syncnet_generated.h -Destination ..\Game\syncnet_generated.h -Force
Copy-Item -LiteralPath .\syncnet_generated.h -Destination ..\Engine\flatbuffers\syncnet_generated.h -Force
Remove-Item -LiteralPath .\syncnet_generated.h

.\flatc.exe --csharp -o . --gen-all syncnet.fbs
Copy-Item -LiteralPath .\syncnet\GameMessages.cs -Destination ..\Client\Assets\Scripts\flatbuffers\syncnet\GameMessages.cs -Force
Copy-Item -LiteralPath .\syncnet\GameMessage.cs -Destination ..\Client\Assets\Scripts\flatbuffers\syncnet\GameMessage.cs -Force
Copy-Item -LiteralPath .\syncnet\TreeDebugDefinition.cs -Destination ..\Client\Assets\Scripts\flatbuffers\syncnet\TreeDebugDefinition.cs -Force
Copy-Item -LiteralPath .\syncnet\TreeDebugNodeChange.cs -Destination ..\Client\Assets\Scripts\flatbuffers\syncnet\TreeDebugNodeChange.cs -Force
Copy-Item -LiteralPath .\syncnet\TreeDebugNodeDefinition.cs -Destination ..\Client\Assets\Scripts\flatbuffers\syncnet\TreeDebugNodeDefinition.cs -Force
Copy-Item -LiteralPath .\syncnet\TreeDebugRuntimeFrame.cs -Destination ..\Client\Assets\Scripts\flatbuffers\syncnet\TreeDebugRuntimeFrame.cs -Force
Copy-Item -LiteralPath .\syncnet\TreeDebugSync.cs -Destination ..\Client\Assets\Scripts\flatbuffers\syncnet\TreeDebugSync.cs -Force
Copy-Item -LiteralPath .\syncnet\TreeNodeStatus.cs -Destination ..\Client\Assets\Scripts\flatbuffers\syncnet\TreeNodeStatus.cs -Force
Copy-Item -LiteralPath .\syncnet\TreeNodeType.cs -Destination ..\Client\Assets\Scripts\flatbuffers\syncnet\TreeNodeType.cs -Force
Remove-Item -LiteralPath .\syncnet -Recurse -Force
cd ..
```

When adding new Unity generated `.cs` files, also add matching `.meta` files with unique GUIDs.

## Git In Sandbox

If `git status` reports dubious ownership because the sandbox user differs from the repo owner, use:

```powershell
git -c safe.directory=D:/projects/GameServerFramework status --short
```
