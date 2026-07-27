using System.Text.Json.Serialization;

namespace AoE1Control.Profiles;

internal sealed class GameProfile
{
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; init; }

    [JsonPropertyName("profileId")]
    public string ProfileId { get; init; } = string.Empty;

    [JsonPropertyName("game")]
    public string Game { get; init; } = string.Empty;

    [JsonPropertyName("edition")]
    public string Edition { get; init; } = string.Empty;

    [JsonPropertyName("executable")]
    public ExecutableProfile Executable { get; init; } = new();

    [JsonPropertyName("session")]
    public SessionProfile Session { get; init; } = new();

    [JsonPropertyName("localPlayer")]
    public LocalPlayerProfile LocalPlayer { get; init; } = new();

    [JsonPropertyName("resources")]
    public ResourceProfile Resources { get; init; } = new();
}

internal sealed class ExecutableProfile
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("sha256")]
    public string Sha256 { get; init; } = string.Empty;
}

internal sealed class SessionProfile
{
    [JsonPropertyName("address")]
    public string Address { get; init; } = string.Empty;

    [JsonPropertyName("activeValue")]
    public uint ActiveValue { get; init; }
}

internal sealed class LocalPlayerProfile
{
    [JsonPropertyName("playerId")]
    public int PlayerId { get; init; } = 1;

    [JsonPropertyName("playerContainer")]
    public PlayerContainerProfile PlayerContainer { get; init; } = new();

    [JsonPropertyName("playerBaseOffset")]
    public string PlayerBaseOffset { get; init; } = string.Empty;

    [JsonPropertyName("resourceOwnerOffset")]
    public string ResourceOwnerOffset { get; init; } = string.Empty;

    [JsonPropertyName("resourceBlockOffset")]
    public string ResourceBlockOffset { get; init; } = string.Empty;
}

internal sealed class PlayerContainerProfile
{
    [JsonPropertyName("moduleOffset")]
    public string ModuleOffset { get; init; } = string.Empty;
}

internal sealed class ResourceProfile
{
    [JsonPropertyName("foodOffset")]
    public string FoodOffset { get; init; } = string.Empty;

    [JsonPropertyName("woodOffset")]
    public string WoodOffset { get; init; } = string.Empty;

    [JsonPropertyName("stoneOffset")]
    public string StoneOffset { get; init; } = string.Empty;

    [JsonPropertyName("goldOffset")]
    public string GoldOffset { get; init; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;
}
