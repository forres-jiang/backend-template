using Newtonsoft.Json;
using System.Collections.Generic;

namespace My.XXX.Contracts.Serialization;

/// <summary>Legacy JSON encoding shared by wire and storage boundary mappings.</summary>
public static class LocalizedNamesJson
{
    public static Dictionary<string, string> Read(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        try { return JsonConvert.DeserializeObject<Dictionary<string, string>>(value); }
        catch (JsonException) { return new(); }
    }
    public static string Write(IReadOnlyDictionary<string, string> values) =>
        values == null ? null : JsonConvert.SerializeObject(values);
}
