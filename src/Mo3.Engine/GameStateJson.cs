using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mo3.Engine;

public static class GameStateJson
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serialize(GameState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return JsonSerializer.Serialize(state, SerializerOptions);
    }

    public static GameState Deserialize(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        var state = JsonSerializer.Deserialize<GameState>(json, SerializerOptions);
        if (state is null)
        {
            throw new InvalidOperationException("Failed to deserialize GameState JSON.");
        }

        return state;
    }
}
