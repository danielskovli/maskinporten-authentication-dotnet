using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SampleCmdApp;

public static class SettingsLoader<T>
{
    public static T Load(string filepath, string jsonPropertyName)
    {
        Debug.Assert(File.Exists(filepath));

        var raw = File.ReadAllText(filepath);
        var parsed = JsonNode.Parse(raw) ?? throw new NotSupportedException($"Unable to parse json data: {raw}");
        return parsed[jsonPropertyName].Deserialize<T>()
            ?? throw new NotSupportedException($"The file {filepath} does not match expected type {typeof(T)}");
    }
}
