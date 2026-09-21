using System;

namespace My.XXX.Services.Models;

/// <summary>Application state used by menu policies; never returned directly by HTTP endpoints.</summary>
public sealed class MenuState
{
    public int Id { get; set; }
    public string DisplayName { get; set; }
    public string DisplayNames { get; set; }
    public string Description { get; set; }
    public string Icon { get; set; }
    public int Number { get; set; }
    public string Url { get; set; }
    public string Component { get; set; }
    public string ControllerName { get; set; }
    public string ActionName { get; set; }
    public string LinkTarget { get; set; }
    public int ParentId { get; set; }
    public bool IsDisplay { get; set; }
    public bool IsAction { get; set; }
    public bool IsDeleted { get; set; }
    public string CreatedBy { get; set; }
    public DateTime CreatedTime { get; set; }
    public string UpdatedBy { get; set; }
    public DateTime? UpdatedTime { get; set; }

    public MenuState Copy() => (MenuState)MemberwiseClone();
}
