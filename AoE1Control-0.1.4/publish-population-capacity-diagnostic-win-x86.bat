@echo off
setlocal

cd /d "%~dp0"

echo [AoE1Control] Publicando PopulationCapacityDiagnostic win-x86...
dotnet publish diagnostics\AoE1Control.PopulationCapacityDiagnostic\AoE1Control.PopulationCapacityDiagnostic.csproj ^
  -c Release ^
  -r win-x86 ^
  --self-contained false ^
  -o artifacts\publish\population-capacity-diagnostic-win-x86

set "EXIT_CODE=%ERRORLEVEL%"

echo.
if not "%EXIT_CODE%"=="0" (
  echo [AoE1Control] Falha na publicacao. Codigo=%EXIT_CODE%
  pause
  exit /b %EXIT_CODE%
)

echo [AoE1Control] Publicacao concluida:
echo %CD%\artifacts\publish\population-capacity-diagnostic-win-x86
pause
