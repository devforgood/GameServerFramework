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

## Windows SDK target (`_WIN32_WINNT`)

`Directory.Build.props` defines `_WIN32_WINNT=0x0A00` (Windows 10) for every C++ project.
Do not remove it and do not re-declare it per project.

Boost.Asio prints this when the macro is missing:

```text
Please define _WIN32_WINNT or _WIN32_WINDOWS appropriately. ...
Assuming _WIN32_WINNT=0x0601 (i.e. Windows 7 target).
```

It is not only noise — Boost then **silently defines `_WIN32_WINNT` as `0x0601`** for that
translation unit. `Engine/Server.h` used to be the only place setting `0x0A00`, and a header
can only win when it is included before Boost. So `Engine.lib` ended up mixing TUs built
against Windows 10 headers with TUs built against Windows 7 headers. The Windows SDK changes
structures and declarations between those versions, so the link succeeds while stitching
together different definitions of the same things.

On the compiler command line the macro precedes every header, so no TU can disagree.

## Zero-warning build

`Debug|x64` and `Release|x64` both rebuild with **0 errors, 0 warnings, and 0 `message :` lines**.
Keep it that way — the log is only useful as a signal while it stays empty.

Check all three when verifying. MSVC and Boost emit some deprecation notices as `message :`
rather than `warning`, so grepping only for `warning` will report a clean build that is not one
(this is exactly how the `boost/bind.hpp` notice survived a "0 warnings" run).

Two of the settings that get you there are intentionally *not* fixes, and are commented as such
in `Directory.Build.props`:

- `_SILENCE_STDEXT_ARR_ITERS_DEPRECATION_WARNING` — spdlog's bundled fmt uses
  `stdext::checked_array_iterator`, which MSVC STL deprecated (C4996 / STL4043). Not ours to fix.
- `CheckEolTargetFramework` / `SuppressTfmSupportBuildWarnings` / `NoWarn=NU1701` — the C# projects
  target `netcoreapp3.1` and `net5.0`, both end-of-life, while referencing 9.0.x packages.
  **The real fix is a TFM migration to net8.0+**; until then the noise is muted. Delete that block
  after migrating.

If a build starts warning again, the usual causes are:

- a source file saved as CP949 instead of UTF-8 (C4828 — the whole repo compiles with `/utf-8`)
- an unused third-party header pulled in for no reason. `Engine/Server.h` included
  `<boost/bind.hpp>` while nothing used `boost::bind`, and it printed a deprecation notice in
  every project that had not defined `BOOST_BIND_GLOBAL_PLACEHOLDERS`. Deleting the include
  removed the notice *and* made that macro unnecessary in all four projects that carried it —
  prefer dropping the dependency over adding another silencing macro.
- stale `*.tlog` folders left in another project's intermediate directory (MSB8028); they are
  build artifacts, so deleting them is safe
- a new C++ project that does not pick up `Directory.Build.props`
- a `Rebuild` started while `x64\Debug\Game.exe` (or a test binary) is still running. Clean
  cannot delete the locked files and warns "access denied" for `game.exe` / `lua51.dll`.
  Nothing is wrong with the tree — stop the server and rebuild.

### Files under `Client/Assets/` are compiled twice

Unity compiles everything under `Client/Assets/`, and several server projects (`core`, `Battle`)
link the same files in. Server-only dependencies must sit inside the
`#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID` / `#else` guard —
including the `using` directives. A `using StackExchange.Redis;` added outside the guard builds
fine on the server and breaks the Unity editor with CS0246, which the solution build never shows.

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

## UnitTest runtime DLLs

vcpkg's applocal deployment copies debug variants of most dependencies into
`UnitTest\x64\Debug` but has repeatedly missed `behaviortree_cppd.dll`, leaving only the
release `behaviortree_cpp.dll`. The build succeeds and `UnitTest.exe` then dies immediately
with exit code `-1073741515` (`STATUS_DLL_NOT_FOUND`) before printing anything.

The Debug|x64 post-build event in `UnitTest.vcxproj` now copies it from `x64\Debug\`, which
`Engine`/`Game` populate. If you hit the same exit code for another library, check with:

```powershell
dumpbin /dependents UnitTest\x64\Debug\UnitTest.exe
```

## Git In Sandbox

If `git status` reports dubious ownership because the sandbox user differs from the repo owner, use:

```powershell
git -c safe.directory=D:/projects/GameServerFramework status --short
```
