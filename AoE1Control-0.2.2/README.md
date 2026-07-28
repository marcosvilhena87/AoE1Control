# AoE1Control 0.2.2 — PlayerStateAvailabilityApi

A indisponibilidade temporária do estado do jogador agora é uma parte formal da API.

## Uso recomendado

```csharp
using AoE1Control;

using PlayerStateApi api =
    PlayerStateApi.Connect();

PlayerStateReadResult result =
    api.TryRead();

if (result.IsAvailable)
{
    PlayerStateSnapshot state =
        result.Snapshot!;
}
else
{
    Console.WriteLine(result.Availability);
}
```

## Estados possíveis

```text
Available
ProcessDisconnected
PlayerContainerUnavailable
PlayerBaseUnavailable
PlayerStateUnavailable
ResourceOwnerUnavailable
ResourceBlockUnavailable
PointerChainChanged
MemoryTemporarilyUnreadable
ImplausibleData
UnknownFailure
```

## Comportamento

Durante mudanças de cenário, menu e carregamento:

```text
TryRead()
```

não lança exceção. Ele retorna um `PlayerStateReadResult` com o motivo semântico.

Para comportamento estrito, ainda existe:

```csharp
PlayerStateSnapshot state =
    api.Read();
```

Nesse caso, a indisponibilidade gera `PlayerStateUnavailableException`, que contém:

```csharp
exception.Availability
```

## Exemplo de transição

```text
Available
ResourceOwnerUnavailable
PlayerBaseUnavailable
PlayerStateUnavailable
Available
```

Os antigos erros derivados de endereços como:

```text
0x00000050
0x00000100
```

agora são classificados antes que a API tente seguir a cadeia.

## Executar diagnóstico

```bat
cd /d C:\Minha_Pasta\Jogos\AoE\AoE1Control\AoE1Control-0.2.2

dotnet run --project diagnostics\AoE1Control.PlayerStateAvailabilityDiagnostic\AoE1Control.PlayerStateAvailabilityDiagnostic.csproj -c Release
```

## Publicar

```bat
publish-player-state-availability-api-win-x86.bat
```

## Objetivo do teste

1. iniciar numa partida;
2. voltar ao menu;
3. carregar outro cenário;
4. observar a sequência de estados sem exceções;
5. encerrar com `Ctrl+C`.
