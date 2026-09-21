namespace My.XXX.Services.Models;

/// <summary>Normalized query criteria. PageIndex is zero-based, independent of HTTP binding.</summary>
public sealed record MenuSearch(string DisplayName, bool? IsAction, bool IsDisplay, int? ParentId, int PageIndex, int PageSize);
