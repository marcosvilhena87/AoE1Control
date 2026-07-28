# AoE1Control 0.2.4 — GamePresenceApi

API mínima para responder somente:

```text
Está na partida?
```

## Estados

```csharp
public enum GamePresenceState
{
    NotInGame,
    InGame
}
```

## Regra

```text
PlayerStateAvailability.Available → InGame
qualquer outro estado             → NotInGame
```

Não diferencia:

- menu;
- carregamento;
- seleção de campanha;
- seleção de cenário;
- telas intermediárias.

Todos são tratados como:

```text
NotInGame
```

## Uso

```csharp
using AoE1Control;

using GamePresenceApi api =
    GamePresenceApi.Connect();

GamePresenceSnapshot presence =
    api.Read();

if (presence.IsInGame)
{
    Console.WriteLine("Está na partida.");
}
else
{
    Console.WriteLine("Não está na partida.");
}
```

Uso direto:

```csharp
bool isInGame =
    api.IsInGame();
```

## Diagnóstico

O diagnóstico imprime somente quando o estado muda:

```text
NotInGame → InGame
InGame → NotInGame
```

Também registra:

- data e hora;
- estado anterior;
- estado atual;
- disponibilidade original do PlayerState;
- duração do estado anterior;
- mensagem da indisponibilidade.

## Executar

```bat
cd /d C:\Minha_Pasta\Jogos\AoE\AoE1Control\AoE1Control-0.2.4

dotnet run --project diagnostics\AoE1Control.GamePresenceDiagnostic\AoE1Control.GamePresenceDiagnostic.csproj -c Release
```

## Publicar

```bat
publish-game-presence-api-win-x86.bat
```

## Arquivo gerado

```text
game-presence\AAAAMMDD-HHMMSS\presence-events.csv
```
