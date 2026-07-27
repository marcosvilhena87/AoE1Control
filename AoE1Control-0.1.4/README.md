# AoE1Control 0.1.4 — PopulationCapacityDiagnostic

Diagnóstico para localizar o campo de capacidade populacional dentro da estrutura:

```text
PlayerState = ReadPointer32(PlayerBase + 0x0100)
```

## Sequência

1. `BASELINE`
2. `FUNDACAO_INICIADA`
3. `FUNDACAO_CANCELADA`
4. `CASA_CONCLUIDA`
5. `CASA_DESTRUIDA`

Durante o teste, não conclua nem perca unidades.

## Padrões procurados

Provável capacidade populacional:

```text
0, 0, aumento, diminuição
```

Contador de fundações/casas:

```text
aumento, diminuição, aumento, 0
```

Contador de casas ativas:

```text
aumento, diminuição, aumento, diminuição
```

Campo que muda na conclusão mas não reverte:

```text
0, 0, aumento, 0
```

## Execução

```bat
cd /d C:\Minha_Pasta\Jogos\AoE\AoE1Control\AoE1Control-0.1.4
dotnet run --project diagnostics\AoE1Control.PopulationCapacityDiagnostic\AoE1Control.PopulationCapacityDiagnostic.csproj -c Release
```

## Saída

```text
population-capacity\AAAAMMDD-HHMMSS\
```

Arquivo principal:

```text
capacity-candidates.csv
```

Também são gerados:

- `captures.csv`
- cinco arquivos `.bin`

## Publicação

```bat
publish-population-capacity-diagnostic-win-x86.bat
```
