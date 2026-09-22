namespace My.XXX.Services.Menus.Models;

/// <summary>规范化的查询条件。PageIndex 从零开始，与 HTTP 绑定无关。</summary>
public sealed record MenuSearch(string DisplayName, bool? IsAction, bool IsDisplay, int? ParentId, int PageIndex, int PageSize);
