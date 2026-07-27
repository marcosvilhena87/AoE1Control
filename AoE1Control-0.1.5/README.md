# AoE1Control 0.1.5 — PlayerStateReaderDiagnostic (corrigido)

Versão corrigida após validar aldeão, unidade militar, barco de pesca e barco de transporte.

## Campos confirmados

```text
PlayerState =
    ReadPointer32(PlayerBase + 0x0100)

PopulationAvailable =
    ReadUInt8(PlayerState + 0x0008)

PopulationCurrent =
    ReadUInt8(PlayerState + 0x0016)

VillagerCount =
    ReadUInt8(PlayerState + 0x004A)

PopulationCapacity =
    PopulationCurrent + PopulationAvailable

NonVillagerCount =
    PopulationCurrent - VillagerCount
```

`NonVillagerCount` inclui qualquer população que não seja aldeão, por exemplo:

- unidades militares;
- barcos de pesca;
- barcos de transporte;
- outras unidades que ocupem população.

## Execução

```bat
cd /d C:\Minha_Pasta\Jogos\AoE\AoE1Control\AoE1Control-0.1.5
dotnet run --project diagnostics\AoE1Control.PlayerStateReaderDiagnostic\AoE1Control.PlayerStateReaderDiagnostic.csproj -c Release
```

## Exemplo

```text
Estado | pop=7/8 | available=1 | villagers=4 | nonVillagers=3
```

## CSV

Colunas:

```text
timestampUtc
session
playerBase
playerState
food
wood
stone
gold
populationCurrent
populationAvailable
populationCapacity
villagerCount
nonVillagerCount
```
