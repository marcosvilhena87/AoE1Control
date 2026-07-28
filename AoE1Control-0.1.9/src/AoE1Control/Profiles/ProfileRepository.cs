using System.Text.Json;

namespace AoE1Control.Profiles;

internal sealed class ProfileRepository
{
    private readonly string _directory;

    internal ProfileRepository(string directory)
    {
        _directory = directory;
    }

    internal IReadOnlyList<GameProfile> LoadAll()
    {
        if (!Directory.Exists(_directory))
            throw new AoE1ControlException(
                $"Diretório de perfis não encontrado: {_directory}");

        JsonSerializerOptions options = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        List<GameProfile> profiles = [];

        foreach (string file in Directory.EnumerateFiles(
                     _directory,
                     "*.json",
                     SearchOption.TopDirectoryOnly))
        {
            try
            {
                string json = File.ReadAllText(file);

                GameProfile profile =
                    JsonSerializer.Deserialize<GameProfile>(
                        json,
                        options)
                    ?? throw new InvalidDataException(
                        "O perfil resultou em null.");

                Validate(profile, file);
                profiles.Add(profile);
            }
            catch (Exception ex)
                when (ex is IOException
                    or UnauthorizedAccessException
                    or JsonException
                    or InvalidDataException)
            {
                throw new AoE1ControlException(
                    $"Falha ao carregar o perfil {file}.",
                    ex);
            }
        }

        if (profiles.Count == 0)
            throw new AoE1ControlException(
                $"Nenhum perfil JSON foi encontrado em {_directory}.");

        return profiles;
    }

    private static void Validate(
        GameProfile profile,
        string file)
    {
        if (profile.SchemaVersion != 1)
            throw new InvalidDataException(
                $"schemaVersion não suportada em {file}.");

        if (string.IsNullOrWhiteSpace(profile.ProfileId))
            throw new InvalidDataException(
                $"profileId ausente em {file}.");

        if (profile.Executable.Sha256.Length != 64)
            throw new InvalidDataException(
                $"SHA-256 inválido em {file}.");

        _ = HexParser.ParseUInt32(profile.Session.Address);
        _ = HexParser.ParseUInt32(
            profile.LocalPlayer.PlayerContainer.ModuleOffset);
        _ = HexParser.ParseUInt32(
            profile.LocalPlayer.PlayerBaseOffset);
        _ = HexParser.ParseUInt32(
            profile.LocalPlayer.ResourceOwnerOffset);
        _ = HexParser.ParseUInt32(
            profile.LocalPlayer.ResourceBlockOffset);
        _ = HexParser.ParseUInt32(
            profile.Resources.FoodOffset);
        _ = HexParser.ParseUInt32(
            profile.Resources.WoodOffset);
        _ = HexParser.ParseUInt32(
            profile.Resources.StoneOffset);
        _ = HexParser.ParseUInt32(
            profile.Resources.GoldOffset);

        if (!string.Equals(
                profile.Resources.Type,
                "float32",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                $"Tipo de recurso não suportado em {file}.");
        }
    }
}
