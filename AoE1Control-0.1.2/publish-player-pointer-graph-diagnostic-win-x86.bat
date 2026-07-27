@echo off
setlocal
cd /d "%~dp0"
dotnet publish diagnostics\AoE1Control.PlayerPointerGraphDiagnostic\AoE1Control.PlayerPointerGraphDiagnostic.csproj ^
  -c Release -r win-x86 --self-contained false ^
  -o artifacts\publish\player-pointer-graph-diagnostic-win-x86
set "EXIT_CODE=%ERRORLEVEL%"
echo.
if not "%EXIT_CODE%"=="0" (
  echo [AoE1Control] Falha na publicacao. Codigo=%EXIT_CODE%
  pause
  exit /b %EXIT_CODE%
)
echo [AoE1Control] Publicado em:
echo %CD%\artifacts\publish\player-pointer-graph-diagnostic-win-x86
pause
