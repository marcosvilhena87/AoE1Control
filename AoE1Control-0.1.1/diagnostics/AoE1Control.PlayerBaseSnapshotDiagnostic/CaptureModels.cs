namespace AoE1Control.PlayerBaseSnapshotDiagnostic;

internal sealed record CapturePhase(
    string Name,
    string Instruction,
    int? ExpectedCurrentDelta,
    int? ExpectedCapacityDelta);

internal sealed record MemoryCapture(
    CapturePhase Phase,
    DateTimeOffset Timestamp,
    uint PlayerBase,
    byte[] Bytes);
