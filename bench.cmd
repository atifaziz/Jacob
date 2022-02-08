@echo off
pushd "%~dp0"
dotnet run -c Release --project bench
popd && exit /b %ERRORLEVEL%
