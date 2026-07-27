# AoE1Control 0.1.0 — ReadOnlyGameApi

API .NET somente leitura para conectar ao **Age of Empires Gold Edition**, validar o executável e obter snapshots dos recursos do jogador local.

## Escopo

- Conexão com `EMPIRES.EXE`
- Validação do executável por SHA-256
- Verificação de sessão ativa
- Resolução automática da cadeia do jogador
- Leitura de comida, madeira, pedra e ouro
- Nenhuma escrita na memória
- Nenhum comando de jogo

## Requisitos

- Windows
- .NET 10 SDK
- Processo x86
- Versão do jogo cadastrada em `profiles`

## Compilar

Abra um Developer Command Prompt do Visual Studio:

```bat
cd /d C:\caminho\AoE1Control-0.1.0
dotnet build AoE1Control.slnx -c Release
```

## Executar o exemplo

```bat
dotnet run --project samples\AoE1Control.Sample\AoE1Control.Sample.csproj -c Release
```

## Publicar win-x86

```bat
publish-win-x86.bat
```

A saída será criada em:

```text
artifacts\publish\win-x86
```

## Uso

```csharp
using AoE1Control;

using IAoE1GameApi game = AoE1GameApi.Connect();

while (game.IsConnected)
{
    if (game.TryGetSnapshot(out GameSnapshot? snapshot))
    {
        ResourceSnapshot resources = snapshot!.LocalPlayer.Resources;

        Console.WriteLine(
            $"Food={resources.Food:0} " +
            $"Wood={resources.Wood:0} " +
            $"Stone={resources.Stone:0} " +
            $"Gold={resources.Gold:0}");
    }

    Thread.Sleep(500);
}
```

## Perfil incluído

O pacote inclui o perfil:

```text
aoe-gold-1abd91e1
```

Hash esperado:

```text
1abd91e18a7034192eab88b841854fad09c77138d3a81a57a57fce3b69b4065d
```

A API rejeita versões desconhecidas por padrão.

## Limitações do 0.1.0

Não inclui:

- unidades;
- edifícios;
- população;
- idade;
- mapa;
- comandos;
- escrita em memória;
- Lua;
- bot;
- overlay.

## Segurança

O processo é aberto somente com:

- `PROCESS_VM_READ`
- `PROCESS_QUERY_INFORMATION`

A biblioteca não chama `WriteProcessMemory`.
