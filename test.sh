#!/usr/bin/env bash
set -e
cd "$(dirname "$0")"
dotnet tool restore
./build.sh
for c in Debug Release; do
    dotnet test --no-build tests -s tests/.runsettings -c $c
done
dotnet reportgenerator "-reports:tests/TestResults/*/coverage.cobertura.xml" -targetdir:tmp -reporttypes:TextSummary
cat tmp/Summary.txt
