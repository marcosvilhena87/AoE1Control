# AoE1Control 0.1.6 — NavalPopulationValidationDiagnostic

Valida separadamente o efeito de:

1. aldeão;
2. unidade militar terrestre;
3. barco de pesca;
4. barco de transporte.

Além dos campos já conhecidos, varre `0x200` bytes do `PlayerState` em busca de contadores navais.

## Campos conhecidos

```text
PlayerState =
    ReadPointer32(PlayerBase + 0x0100)

PopulationAvailable =
    ReadUInt8(PlayerState + 0x0008)

PopulationCurrent =
    ReadUInt8(PlayerState + 0x0016)

VillagerCount =
    ReadUInt8(PlayerState + 0x004A)
```

## Sequência

```text
BASELINE
ALDEAO_CONCLUIDO
MILITAR_CONCLUIDO
BARCO_PESCA_CONCLUIDO
BARCO_TRANSPORTE_CONCLUIDO
```

Use exatamente uma unidade em cada fase.

## Execução

```bat
cd /d C:\Minha_Pasta\Jogos\AoE\AoE1Control\AoE1Control-0.1.6
dotnet run --project diagnostics\AoE1Control.NavalPopulationValidationDiagnostic\AoE1Control.NavalPopulationValidationDiagnostic.csproj -c Release
```

## Saída

```text
naval-population-validation\AAAAMMDD-HHMMSS\
```

Arquivos:

- `captures.csv`
- `summary.txt`
- `naval-candidates.csv`
- cinco dumps `.bin`

## Padrões procurados

```text
Aldeões:              +1, 0, 0, 0
População terrestre:  +1, +1, 0, 0
Militar terrestre:     0, +1, 0, 0
Barco de pesca:         0, 0, +1, 0
Barco de transporte:    0, 0, 0, +1
População naval:        0, 0, +1, +1
Total de unidades:     +1, +1, +1, +1
```

## Publicação

```bat
publish-naval-population-validation-diagnostic-win-x86.bat
```
