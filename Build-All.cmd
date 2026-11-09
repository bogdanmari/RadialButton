@echo off
pushd "%~dp0"
dotnet build RadialButton.sln -c Release
if errorlevel 1 goto failed
dotnet build RadialButton.sln -c Legacy
if errorlevel 1 goto failed
popd
exit /b 0
:failed
popd
exit /b 1
