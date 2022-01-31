@echo off
pushd "%~dp0"
dotnet tool restore ^
 && call build ^
 && call :test Debug ^
 && call :test Release ^
 && dotnet reportgenerator -reports:.\tests\TestResults\*\coverage.cobertura.xml -targetdir:tmp -reporttypes:TextSummary ^
 && type tmp\Summary.txt
popd && exit /b %ERRORLEVEL%

:test
dotnet test --no-build -s tests\.runsettings tests -c %*
goto :EOF
