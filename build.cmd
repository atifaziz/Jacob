@echo off
pushd "%~dp0"
    dotnet restore ^
 && dotnet build --no-restore -c Debug ^
 && dotnet build --no-restore -c Release
popd && exit /b %ERRORLEVEL%
