using AoE1Control.Internal;
using AoE1Control.Profiles;

namespace AoE1Control;

/// <summary>
/// Ponto de entrada da ReadOnlyGameApi.
/// </summary>
public static class AoE1GameApi
{
    /// <summary>
    /// Conecta ao processo do Age of Empires e valida seu perfil.
    /// </summary>
    public static IAoE1GameApi Connect(AoE1ControlOptions? options = null)
    {
        options ??= new AoE1ControlOptions();

        string profilesDirectory =
            options.ProfilesDirectory
            ?? Path.Combine(AppContext.BaseDirectory, "profiles");

        ProfileRepository repository = new(profilesDirectory);
        IReadOnlyList<GameProfile> profiles = repository.LoadAll();

        GameConnection connection = GameConnection.Connect(options, profiles);

        try
        {
            GameSessionReader sessionReader =
                new(connection.Memory, connection.Profile);

            PointerChainResolver pointerResolver =
                new(connection.Memory, connection.ModuleBase, connection.Profile);

            LocalPlayerReader playerReader =
                new(connection.Memory, pointerResolver, connection.Profile);

            return new Internal.GameApi(
                connection,
                sessionReader,
                playerReader);
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }
}
