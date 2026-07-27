# AoE1Control 0.1.3 — PopulationFieldValidationDiagnostic

Valida os dois campos encontrados no `0.1.2`:

```text
PlayerState = ReadPointer32(PlayerBase + 0x0100)

Candidate A = UInt8(PlayerState + 0x0016)
Candidate B = UInt8(PlayerState + 0x004A)
```

## Objetivo

Descobrir qual campo representa:

- quantidade de aldeões;
- população total;
- ou outro contador.

## Sequência do teste

1. `BASELINE`
2. concluir exatamente um aldeão;
3. concluir exatamente uma unidade militar;
4. perder exatamente a unidade militar;
5. perder exatamente um aldeão.

## Padrões esperados

Contagem de aldeões:

```text
+1, 0, 0, -1
```

População total:

```text
+1, +1, -1, -1
```

Contagem militar:

```text
0, +1, -1, 0
```

## Executar

```bat
cd /d C:\Minha_Pasta\Jogos\AoE\AoE1Control\AoE1Control-0.1.3
dotnet run --project diagnostics\AoE1Control.PopulationFieldValidationDiagnostic\AoE1Control.PopulationFieldValidationDiagnostic.csproj -c Release
```

## Arquivos gerados

```text
population-field-validation\AAAAMMDD-HHMMSS\
```

Arquivos:

- `samples.csv`
- `analysis.txt`

## Publicação

```bat
publish-population-field-validation-diagnostic-win-x86.bat
```

Saída:

```text
artifacts\publish\population-field-validation-diagnostic-win-x86
```
