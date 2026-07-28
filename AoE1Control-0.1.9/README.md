# AoE1Control 0.1.9 — UnitCounterValidationDiagnostic (corrigido)

## Correções

- adiciona `using AoE1Control.Internal;`;
- corrige os erros `CS0246` relacionados a `GameConnection`;
- remove `Prefer32Bit`, que não tem efeito em .NET 10 e gerava `NETSDK1189`;
- mantém `PlatformTarget=x86` e publicação `win-x86`.

## Publicar

```bat
cd /d C:\Minha_Pasta\Jogos\AoE\AoE1Control\AoE1Control-0.1.9
publish-unit-counter-validation-diagnostic-win-x86.bat
```

## Executar sem publicar

```bat
dotnet run --project diagnostics\AoE1Control.UnitCounterValidationDiagnostic\AoE1Control.UnitCounterValidationDiagnostic.csproj -c Release
```
