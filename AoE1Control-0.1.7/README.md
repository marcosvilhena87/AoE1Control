# AoE1Control 0.1.7 — CivilianPopulationValidationDiagnostic (corrigido)

Esta revisão corrige o encerramento quando o jogo ainda está carregando a partida.

## Correção

Antes:

```text
pressiona ENTER
faz uma única verificação
encerra se a sessão ainda não estiver ativa
```

Agora:

```text
aguarda continuamente
verifica a cada 500 ms
continua automaticamente quando a sessão ficar ativa
```

Não é mais necessário acertar o momento exato do `ENTER`.

## Execução

```bat
cd /d C:\Minha_Pasta\Jogos\AoE\AoE1Control\AoE1Control-0.1.7
dotnet run --project diagnostics\AoE1Control.CivilianPopulationValidationDiagnostic\AoE1Control.CivilianPopulationValidationDiagnostic.csproj -c Release
```

A saída inicial esperada é:

```text
Aguardando partida ativa...
Sessao ativa detectada.
```

Depois começa a fase `BASELINE`.
