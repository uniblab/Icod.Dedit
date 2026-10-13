@echo off
setlocal
cd /d "%~dp0"

dotnet restore Icod.Dedit.sln
if errorlevel 1 exit /b %errorlevel%
dotnet build Icod.Dedit.sln -c Release --no-restore
if errorlevel 1 exit /b %errorlevel%
dotnet test tests\Icod.Dedit.Tests\Icod.Dedit.Tests.csproj -c Release --no-build --no-restore
if errorlevel 1 exit /b %errorlevel%
exit /b 0
