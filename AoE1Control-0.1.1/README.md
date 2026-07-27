# AoE1Control 0.1.1 — PlayerBaseSnapshotDiagnostic

Diagnóstico corrigido para o cenário em que o jogador começa com **3 aldeões** e atinge o limite populacional ao concluir o quarto.

Não é necessário enxergar a população no formato `5/8`.

## Sequência usada

```text
BASELINE
POPULACAO_MAIS_1
CAPACIDADE_AUMENTOU
POPULACAO_MAIS_1_NOVAMENTE
POPULACAO_MENOS_1
```

Procedimento:

1. Comece com 3 aldeões e limite 4.
2. Conclua o quarto aldeão.
3. Construa e conclua uma casa.
4. Conclua o quinto aldeão.
5. Faça exatamente um aldeão morrer.

## Padrões procurados

População atual:

```text
+1, 0, +1, -1
```

Capacidade populacional:

```text
0, aumento positivo, 0, 0
```

O diagnóstico não presume o aumento exato fornecido pela casa.

## Execução

```bat
cd /d C:\Minha_Pasta\Jogos\AoE\AoE1Control\AoE1Control-0.1.1
dotnet run --project diagnostics\AoE1Control.PlayerBaseSnapshotDiagnostic\AoE1Control.PlayerBaseSnapshotDiagnostic.csproj -c Release
```

Espere cada ação terminar completamente antes de pressionar `ENTER`.

## Arquivo principal

```text
population-delta-candidates.csv
```

Também serão gerados:

- `decoded-changes.csv`
- `changed-bytes.csv`
- `captures.csv`
- um dump `.bin` de cada fase

## Publicação win-x86

```bat
publish-player-base-diagnostic-win-x86.bat
```

Saída:

```text
artifacts\publish\player-base-diagnostic-win-x86
```
