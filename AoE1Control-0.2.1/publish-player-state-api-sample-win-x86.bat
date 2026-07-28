@echo off
setlocal

cd /d "%~dp0"

echo [AoE1Control] Publicando win-x86...
dotnet publish samples\AoE1Control.PlayerStateSample\AoE1Control.PlayerStateSample.csproj ^
  -c Release ^
  -r win-x86 ^
  --self-contained false ^
  -o artifacts\publish\player-state-api-sample-win-x86

set "EXIT_CODE=%ERRORLEVEL%"

echo.
if not "%EXIT_CODE%"=="0" (
  echo [AoE1Control] Falha na publicacao. Codigo=%EXIT_CODE%
  pause
  exit /b %EXIT_CODE%
)

echo [AoE1Control] Publicacao concluida:
echo %CD%\artifacts\publish\player-state-api-sample-win-x86
pause
