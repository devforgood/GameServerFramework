@echo off
pushd %~dp0

python generate.py
if errorlevel 1 (
    popd
    exit /b %errorlevel%
)

popd
