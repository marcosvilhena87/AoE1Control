# AoE1Control 0.2.1 — PlayerStateApi StabilityDiagnostic

Diagnóstico de estabilidade da `PlayerStateApi`.

## O que mede

- tentativas de leitura;
- snapshots válidos;
- snapshots descartados;
- taxa de sucesso;
- sequências de falhas consecutivas;
- duração de cada recuperação;
- maior período de instabilidade;
- mudanças efetivas de estado;
- cadeia completa das exceções internas;
- troca dos endereços `PlayerContainer`, `PlayerBase`, `PlayerState` e `ResourceBlock`.

## Console limpo

A saída não usa `Console.Write("\r")`.

Uma linha é impressa somente quando:

- algum estado muda;
- um snapshot é descartado;
- a API se recupera;
- o diagnóstico é encerrado.

## Arquivos

```text
player-state-stability\AAAAMMDD-HHMMSS\
├── events.csv
├── state-changes.csv
└── summary.txt
```

### `events.csv`

Registra:

```text
UNSTABLE_STARTED
SNAPSHOT_DISCARDED
RECOVERED
FATAL
```

Inclui o tipo e a mensagem da exceção interna.

### `state-changes.csv`

Grava somente mudanças reais:

- população;
- contadores de unidade;
- recursos;
- endereços resolvidos.

### `summary.txt`

Resume:

- taxa de sucesso;
- quantidade de descartes;
- recuperações;
- maior sequência de falhas;
- tempo médio e máximo para recuperação.

## Executar

```bat
cd /d C:\Minha_Pasta\Jogos\AoE\AoE1Control\AoE1Control-0.2.1

dotnet run --project diagnostics\AoE1Control.PlayerStateApi.StabilityDiagnostic\AoE1Control.PlayerStateApi.StabilityDiagnostic.csproj -c Release
```

Encerre com:

```text
Ctrl+C
```

## Publicar

```bat
publish-player-state-api-stability-diagnostic-win-x86.bat
```

## Teste recomendado

1. iniciar numa partida estável;
2. alterar recursos ou criar uma unidade;
3. sair para o menu;
4. carregar `Opening Moves`;
5. permanecer alguns segundos;
6. trocar novamente de cenário;
7. encerrar com `Ctrl+C`.

O resultado mostrará exatamente quanto tempo a API levou para se recuperar em cada transição.
