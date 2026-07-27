using System.Globalization;
using System.Text;
using AoE1Control;
using AoE1Control.Internal;
using AoE1Control.Profiles;

namespace AoE1Control.PlayerPointerGraphDiagnostic;

internal static class Program
{
    private const int SourceSize = 0x800;
    private const int TargetSize = 0x400;

    private static readonly (string Name, string Instruction)[] Phases =
    [
        ("BASELINE", "Comece com 3 aldeões e limite 4. Não altere a população."),
        ("POPULACAO_MAIS_1", "Conclua exatamente um aldeão."),
        ("CAPACIDADE_AUMENTOU", "Conclua exatamente uma casa, sem concluir ou perder unidades."),
        ("POPULACAO_MAIS_1_NOVAMENTE", "Conclua exatamente mais um aldeão."),
        ("POPULACAO_MENOS_1", "Faça exatamente um aldeão morrer.")
    ];

    private static int Main()
    {
        Console.WriteLine("[AoE1Control] Carregado | versao=0.1.2 | diagnostico=PlayerPointerGraphDiagnostic");
        Console.WriteLine($"[PlayerPointerGraphDiagnostic] Configuracao | sourceSize=0x{SourceSize:X} | targetSize=0x{TargetSize:X} | profundidade=1");

        try
        {
            string profilesPath = Path.Combine(AppContext.BaseDirectory, "profiles");
            IReadOnlyList<GameProfile> profiles = new ProfileRepository(profilesPath).LoadAll();
            using GameConnection connection = GameConnection.Connect(new AoE1ControlOptions(), profiles);

            GameSessionReader session = new(connection.Memory, connection.Profile);
            PointerChainResolver resolver = new(connection.Memory, connection.ModuleBase, connection.Profile);

            Console.WriteLine(
                $"[PlayerPointerGraphDiagnostic] Processo conectado | perfil={connection.Profile.ProfileId} | moduloBase=0x{connection.ModuleBase.ToInt64():X8}");

            if (!session.IsSessionActive())
            {
                Console.WriteLine("Entre em uma partida e pressione ENTER.");
                Console.ReadLine();
            }

            if (!session.IsSessionActive())
                throw new GameSessionNotActiveException("A sessão ainda não está ativa.");

            var captures = new List<Capture>();

            foreach (var phase in Phases)
            {
                Console.WriteLine();
                Console.WriteLine($"[PlayerPointerGraphDiagnostic] FASE | nome={phase.Name}");
                Console.WriteLine(phase.Instruction);
                Console.WriteLine("Espere a ação terminar completamente e pressione ENTER.");
                Console.ReadLine();

                resolver.Invalidate();
                ResolvedPlayerPointers resolved = resolver.Resolve();
                Console.WriteLine(
                    $"[PlayerPointerGraphDiagnostic] PlayerBase resolvido | endereco=0x{resolved.PlayerBase:X8}");

                byte[] source =
                    connection.Memory.ReadAvailableBytes(
                        resolved.PlayerBase,
                        SourceSize);

                Console.WriteLine(
                    $"[PlayerPointerGraphDiagnostic] Origem capturada | bytes=0x{source.Length:X}");

                var targets = new Dictionary<int, Target>();
                var seenAddresses = new HashSet<uint>();

                for (int offset = 0; offset + 4 <= source.Length; offset += 4)
                {
                    uint address = BitConverter.ToUInt32(source, offset);

                    // Faixa plausível de endereço de usuário para processo x86.
                    // Exclui valores pequenos, sentinelas e endereços que podem
                    // provocar overflow ao serem tratados como ponteiros.
                    if (address < 0x00010000 ||
                        address >= 0x80000000 ||
                        (address & 3) != 0)
                    {
                        continue;
                    }

                    if (!seenAddresses.Add(address))
                        continue;

                    try
                    {
                        if (!connection.Memory.CanRead(address, 4))
                            continue;

                        byte[] targetBytes =
                            connection.Memory.ReadAvailableBytes(
                                address,
                                TargetSize);

                        if (targetBytes.Length < 4)
                            continue;

                        targets[offset] =
                            new Target(
                                offset,
                                address,
                                targetBytes);
                    }
                    catch (Exception ex)
                        when (ex is MemoryReadException
                            or OverflowException
                            or ArgumentOutOfRangeException)
                    {
                        Console.WriteLine(
                            $"[PlayerPointerGraphDiagnostic] Ponteiro ignorado | " +
                            $"sourceOffset=0x{offset:X4} | " +
                            $"target=0x{address:X8} | " +
                            $"motivo={ex.GetType().Name}");

                        continue;
                    }
                }

                Console.WriteLine(
                    $"[PlayerPointerGraphDiagnostic] Varredura concluida | " +
                    $"valoresExaminados={source.Length / 4} | " +
                    $"destinosValidos={targets.Count}");

                captures.Add(new Capture(
                    phase.Name,
                    DateTimeOffset.UtcNow,
                    resolved.PlayerBase,
                    targets));

                Console.WriteLine(
                    $"[PlayerPointerGraphDiagnostic] Captura | " +
                    $"playerBase=0x{resolved.PlayerBase:X8} | " +
                    $"destinos={targets.Count}");
            }

            string output = Path.Combine(AppContext.BaseDirectory, "player-pointer-graph", DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            Directory.CreateDirectory(output);
            WriteCaptures(output, captures);
            WriteTargets(output, captures);
            WriteCandidates(output, captures);

            Console.WriteLine();
            Console.WriteLine("[PlayerPointerGraphDiagnostic] RESULTADO | status=SUCESSO");
            Console.WriteLine($"[PlayerPointerGraphDiagnostic] Arquivos | diretorio={output}");
            Console.WriteLine("[PlayerPointerGraphDiagnostic] Consulte primeiro: pointer-target-population-candidates.csv");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine(
                $"[PlayerPointerGraphDiagnostic] ERRO_FATAL | " +
                $"tipo={ex.GetType().Name} | " +
                $"mensagem={ex.Message.Replace('\r',' ').Replace('\n',' ')}");

            Console.Error.WriteLine(
                "[PlayerPointerGraphDiagnostic] Pressione ENTER para encerrar.");

            Console.ReadLine();
            return 1;
        }
    }

    private static void WriteCaptures(string output, IReadOnlyList<Capture> captures)
    {
        var csv = new StringBuilder("phase,timestampUtc,playerBase,targetCount\n");
        foreach (Capture c in captures)
            csv.AppendLine($"{c.Name},{c.Timestamp.UtcDateTime:O},0x{c.PlayerBase:X8},{c.Targets.Count}");
        File.WriteAllText(Path.Combine(output, "captures.csv"), csv.ToString(), new UTF8Encoding(false));
    }

    private static void WriteTargets(string output, IReadOnlyList<Capture> captures)
    {
        var csv = new StringBuilder("phase,sourceOffsetHex,targetAddress,targetSize\n");
        string dumps = Path.Combine(output, "targets");
        Directory.CreateDirectory(dumps);

        foreach (Capture c in captures)
        {
            string phaseDir = Path.Combine(dumps, c.Name);
            Directory.CreateDirectory(phaseDir);

            foreach (Target t in c.Targets.Values)
            {
                csv.AppendLine($"{c.Name},0x{t.SourceOffset:X4},0x{t.Address:X8},{t.Bytes.Length}");
                File.WriteAllBytes(Path.Combine(phaseDir, $"source-0x{t.SourceOffset:X4}-target-0x{t.Address:X8}.bin"), t.Bytes);
            }
        }

        File.WriteAllText(Path.Combine(output, "pointer-targets.csv"), csv.ToString(), new UTF8Encoding(false));
    }

    private static void WriteCandidates(string output, IReadOnlyList<Capture> captures)
    {
        var csv = new StringBuilder("field,sourceOffsetHex,targetOffsetHex,type,values,deltas,status\n");
        string[] types = ["UInt8", "Int16", "UInt16", "Int32", "UInt32", "Float32"];

        HashSet<int> common = captures[0].Targets.Keys.ToHashSet();
        foreach (Capture c in captures.Skip(1))
            common.IntersectWith(c.Targets.Keys);

        foreach (int sourceOffset in common.OrderBy(x => x))
        {
            Target[] targets = captures.Select(c => c.Targets[sourceOffset]).ToArray();

            int commonLength =
                targets.Min(t => t.Bytes.Length);

            for (int offset = 0; offset < commonLength; offset++)
            {
                foreach (string type in types)
                {
                    int size = type is "UInt8" ? 1 : type is "Int16" or "UInt16" ? 2 : 4;
                    if (offset + size > commonLength)
                        continue;

                    double[] values = targets.Select(t => Read(t.Bytes, offset, type)).ToArray();
                    if (values.Any(double.IsNaN) || values.Any(double.IsInfinity))
                        continue;

                    double[] d = [values[1]-values[0], values[2]-values[1], values[3]-values[2], values[4]-values[3]];
                    bool current = Near(d[0],1) && Near(d[1],0) && Near(d[2],1) && Near(d[3],-1);
                    bool capacity = Near(d[0],0) && d[1] > 0.001 && Near(d[2],0) && Near(d[3],0);

                    if (!current && !capacity)
                        continue;

                    string valueText = string.Join("|", values.Select(v => v.ToString("0.###", CultureInfo.InvariantCulture)));
                    string deltaText = string.Join("|", d.Select(v => v.ToString("+0.###;-0.###;0", CultureInfo.InvariantCulture)));
                    if (current)
                        csv.AppendLine($"populationCurrent,0x{sourceOffset:X4},0x{offset:X4},{type},{valueText},{deltaText},PADRAO_EXATO");
                    if (capacity)
                        csv.AppendLine($"populationCapacity,0x{sourceOffset:X4},0x{offset:X4},{type},{valueText},{deltaText},PADRAO_EXATO");
                }
            }
        }

        File.WriteAllText(Path.Combine(output, "pointer-target-population-candidates.csv"), csv.ToString(), new UTF8Encoding(false));
    }

    private static double Read(byte[] b, int o, string type) => type switch
    {
        "UInt8" => b[o],
        "Int16" => BitConverter.ToInt16(b, o),
        "UInt16" => BitConverter.ToUInt16(b, o),
        "Int32" => BitConverter.ToInt32(b, o),
        "UInt32" => BitConverter.ToUInt32(b, o),
        "Float32" => BitConverter.ToSingle(b, o),
        _ => double.NaN
    };

    private static bool Near(double a, double b) => Math.Abs(a - b) <= 0.001;

    private sealed record Target(int SourceOffset, uint Address, byte[] Bytes);
    private sealed record Capture(string Name, DateTimeOffset Timestamp, uint PlayerBase, Dictionary<int, Target> Targets);
}
