@echo off
setlocal
pushd "%~dp0"
set VERSION_SUFFIX=
if not "%~1"=="" set VERSION_SUFFIX=--version-suffix %~1
call build && dotnet pack --no-build -c Release %VERSION_SUFFIX% src
popd && exit /b %ERRORLEVEL%
