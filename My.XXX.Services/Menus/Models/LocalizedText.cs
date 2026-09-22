using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace My.XXX.Services.Menus.Models;

/// <summary>Immutable localized values; independent of wire/storage encoding.</summary>
public sealed class LocalizedText
{
    public IReadOnlyDictionary<string, string> Values { get; }
    public LocalizedText(IEnumerable<KeyValuePair<string, string>> values = null)
    {
        var copy = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (values != null) foreach (var pair in values) copy[pair.Key] = pair.Value;
        Values = new ReadOnlyDictionary<string, string>(copy);
    }
    public LocalizedText With(string culture, string name)
    {
        var copy = new Dictionary<string, string>(Values, StringComparer.OrdinalIgnoreCase);
        copy[culture] = name?.Trim();
        return new LocalizedText(copy);
    }
}
