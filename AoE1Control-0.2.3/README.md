# AoE1Control 0.2.3 — GameSessionStateApi

API pública de alto nível para identificar o estado atual da sessão do jogo.

## Estados

```text
Unknown
Disconnected
Menu
Loading
InGame
```

## Uso

```csharp
using AoE1Control;

using GameSessionStateApi api =
    GameSessionStateApi.Connect();

GameSessionSnapshot session =
    api.Read();

Console.WriteLine(session.State);

if (session.IsInGame)
{
    PlayerStateSnapshot player =
        session.PlayerState!;
}
```

## Mapeamento inicial

```text
Available                    → InGame
ProcessDisconnected          → Disconnected
PlayerContainerUnavailable   → Menu
PlayerBaseUnavailable        → Loading
PlayerStateUnavailable       → Loading
ResourceOwnerUnavailable     → Loading
ResourceBlockUnavailable     → Loading
PointerChainChanged          → Loading
MemoryTemporarilyUnreadable  → Loading
ImplausibleData              → Loading
outros                       → Unknown
```

## Observação importante

O estado `Menu` ainda é uma classificação inicial.

Ele só é retornado quando:

```text
PlayerContainerUnavailable
```

Se o jogo mantiver `PlayerContainer` válido no menu principal, será necessário um diagnóstico específico para diferenciar `Menu` de `Loading`.

## Diagnóstico

```bat
cd /d C:\Minha_Pasta\Jogos\AoE\AoE1Control\AoE1Control-0.2.3

dotnet run --project diagnostics\AoE1Control.GameSessionStateDiagnostic\AoE1Control.GameSessionStateDiagnostic.csproj -c Release
```

## Publicação

```bat
publish-game-session-state-api-win-x86.bat
```

## Teste recomendado

1. iniciar numa partida;
2. sair para o menu;
3. aguardar alguns segundos;
4. carregar outro cenário;
5. observar a sequência;
6. encerrar com `Ctrl+C`.

Exemplo esperado:

```text
InGame
Loading
Menu
Loading
InGame
```

Dependendo da forma como o jogo desmonta a cadeia, `Menu` pode não aparecer. Esse resultado será útil para o próximo refinamento.
