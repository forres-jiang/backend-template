using My.XXX.Services.Menus.Models;
namespace My.XXX.Services.Menus.Policies;

public static class MenuDisplayNames
{
    public static string Get(LocalizedText value, string culture, string fallback) =>
        value != null && value.Values.TryGetValue(culture, out var name) && !string.IsNullOrWhiteSpace(name) ? name : fallback;
    public static LocalizedText Set(LocalizedText value, string culture, string name) =>
        (value ?? new LocalizedText()).With(culture, name);
}
