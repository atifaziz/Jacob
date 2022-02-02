@echo off
pushd "%~dp0"
    call build ^
 && call :test Debug ^
 && call :test Release
popd && exit /b %ERRORLEVEL%

:test
dotnet test --no-build -s tests\.runsettings tests -c %*
goto :EOF
