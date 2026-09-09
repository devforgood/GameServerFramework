flatc.exe --csharp -o ../Client/Assets/Scripts/flatbuffers syncnet.fbs
rem The C++ output the engine actually compiles lives in Engine/flatbuffers.
rem This used to generate into Game only, so regenerating here left the server header stale.
flatc.exe --cpp -o ../Engine/flatbuffers syncnet.fbs
copy /Y ..\Engine\flatbuffers\syncnet_generated.h ..\Game\syncnet_generated.h
pause
