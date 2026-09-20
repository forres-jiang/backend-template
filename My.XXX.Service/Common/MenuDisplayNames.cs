using Newtonsoft.Json;
using System;
using System.Collections.Generic;
namespace My.XXX.Service.Common;

public static class MenuDisplayNames
{
    private static Dictionary<string, string> Read(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return new(StringComparer.OrdinalIgnoreCase);
        try
        {
            var names = JsonConvert.DeserializeObject<Dictionary<string, string>>(value);
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (names != null) foreach (var pair in names) result[pair.Key] = pair.Value;
            return result;
        }
        catch (JsonException) { return new(StringComparer.OrdinalIgnoreCase); }
    }

    public static string Get(string value, string culture, string fallback) =>
        Read(value).TryGetValue(culture, out var name) && !string.IsNullOrWhiteSpace(name) ? name : fallback;

    public static string Set(string value, string culture, string name)
    {
        var names = Read(value);
        names[culture] = name?.Trim();
        return JsonConvert.SerializeObject(names);
    }
}
