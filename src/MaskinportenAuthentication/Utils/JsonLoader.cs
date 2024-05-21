using System.Text.Json;

namespace MaskinportenAuthentication.Utils;

public static class JsonLoader<T>
{
    public static T LoadFile(string filepath)
    {
        var raw = File.ReadAllText(filepath);
        return JsonSerializer.Deserialize<T>(raw)
            ?? throw new NotSupportedException($"File {filepath} does not match expected type {typeof(T)}");
    }

    public static async Task<T> LoadFileAsync(string filepath)
    {
        var raw = await File.ReadAllTextAsync(filepath);
        return JsonSerializer.Deserialize<T>(raw)
            ?? throw new NotSupportedException($"File {filepath} does not match expected type {typeof(T)}");
    }
}
