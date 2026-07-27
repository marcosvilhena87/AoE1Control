@echo off
setlocal

cd /d "%~dp0"

echo [AoE1Control] Publicando PlayerBaseSnapshotDiagnostic win-x86...
dotnet publish diagnostics\AoE1Control.PlayerBaseSnapshotDiagnostic\AoE1Control.PlayerBaseSnapshotDiagnostic.csproj ^
  -c Release ^
  -r win-x86 ^
  --self-contained false ^
  -o artifacts\publish\player-base-diagnostic-win-x86

set "EXIT_CODE=%ERRORLEVEL%"

echo.
if not "%EXIT_CODE%"=="0" (
  echo [AoE1Control] Falha na publicacao. Codigo=%EXIT_CODE%
  pause
  exit /b %EXIT_CODE%
)

echo [AoE1Control] Publicacao concluida:
echo %CD%\artifacts\publish\player-base-diagnostic-win-x86
pause
