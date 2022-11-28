@echo off
pushd "%~dp0"
dotnet run -c Release -f net7.0 --runtimes nativeaot7.0 net7.0 net6.0 --project bench
popd && exit /b %ERRORLEVEL%
