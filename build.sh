#!/usr/bin/env sh
set -eu

cd "$(dirname "$0")"
dotnet restore Icod.Dedit.sln
dotnet build Icod.Dedit.sln -c Release --no-restore
dotnet test tests/Icod.Dedit.Tests/Icod.Dedit.Tests.csproj -c Release --no-build --no-restore
