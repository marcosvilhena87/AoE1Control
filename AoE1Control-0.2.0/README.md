# AoE1Control 0.2.0 — PlayerStateApi (corrigido)

## Correções de compilação

### CS0051

Antes, o construtor público de `PlayerStateReader` recebia o tipo interno `GameConnection`.

Agora:

```csharp
internal PlayerStateReader(GameConnection connection)
```

O consumidor usa apenas:

```csharp
using PlayerStateApi api = PlayerStateApi.Connect();
PlayerStateSnapshot state = api.Read();
```

### CS0246

Foi adicionado ao `PlayerStateApi.cs`:

```csharp
using AoE1Control.Internal;
```

### NETSDK1189

`Prefer32Bit` foi removido de todos os projetos. A compilação continua x86 por:

```xml
<PlatformTarget>x86</PlatformTarget>
```

e pela publicação:

```text
-r win-x86
```

## Publicar

```bat
cd /d C:\Minha_Pasta\Jogos\AoE\AoE1Control\AoE1Control-0.2.0
publish-win-x86.bat
```

Também pode usar:

```bat
publish-player-state-api-sample-win-x86.bat
```
