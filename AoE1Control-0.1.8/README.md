# AoE1Control 0.1.8 — UnitCategoryValidationDiagnostic (Int8 final)

Correção efetivamente aplicada ao código-fonte.

## Campo corrigido

```text
PopulationAvailable =
    ReadInt8(PlayerState + 0x0008)
```

No início de `Opening Moves`:

```text
PopulationCurrent   = 2
PopulationAvailable = -2
PopulationCapacity  = 0
```

O byte bruto `254` é interpretado como `-2`.

## Confirmação visual

A primeira linha de campos deve mostrar:

```text
available=Int8(+0x0008)
```

Se aparecer `UInt8`, ainda está sendo executada uma pasta/build antigo.

## Execução limpa

```bat
cd /d C:\Minha_Pasta\Jogos\AoE\AoE1Control\AoE1Control-0.1.8
rmdir /s /q diagnostics\AoE1Control.UnitCategoryValidationDiagnostic\bin 2>nul
rmdir /s /q diagnostics\AoE1Control.UnitCategoryValidationDiagnostic\obj 2>nul
dotnet run --project diagnostics\AoE1Control.UnitCategoryValidationDiagnostic\AoE1Control.UnitCategoryValidationDiagnostic.csproj -c Release
```
