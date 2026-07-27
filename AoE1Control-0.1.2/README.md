# AoE1Control 0.1.2 — PlayerPointerGraphDiagnostic

Revisão final para corrigir `OverflowException` durante a primeira captura.

## Correções

- aceita somente endereços plausíveis de usuário x86:
  - mínimo `0x00010000`;
  - máximo exclusivo `0x80000000`;
- ignora valores desalinhados;
- captura cada ponteiro dentro de um bloco protegido;
- `OverflowException` em um candidato não encerra o diagnóstico;
- impede overflow ao avançar a leitura fragmentada;
- mostra os ponteiros descartados e o motivo.

## Executar

```bat
cd /d C:\Minha_Pasta\Jogos\AoE\AoE1Control\AoE1Control-0.1.2
dotnet run --project diagnostics\AoE1Control.PlayerPointerGraphDiagnostic\AoE1Control.PlayerPointerGraphDiagnostic.csproj -c Release
```

Após a captura da origem, o programa deve continuar com:

```text
Varredura concluida | valoresExaminados=512 | destinosValidos=...
Captura | playerBase=0x... | destinos=...
```

Ponteiros inválidos podem aparecer como:

```text
Ponteiro ignorado | sourceOffset=0x... | target=0x... | motivo=OverflowException
```

Isso é esperado e não interrompe a execução.
