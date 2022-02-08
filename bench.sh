#!/usr/bin/env bash
set -e
cd "$(dirname "$0")"
dotnet run -c Release --project bench
