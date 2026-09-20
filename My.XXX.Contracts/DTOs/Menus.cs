using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace My.XXX.Service.DTOs
{
    public interface ILocalizedMenuDto
    {
        string DisplayName { get; set; }
        string DisplayNames { get; set; }
    }

    // Legacy response shape. Internal menu state lives in Service.Models.
    public class MenuBase : ILocalizedMenuDto
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
    }

    public class MenuBaseDto
    {
        public int Id { get; set; }
        public string DisplayName { get; set; }
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
    }

    /// <summary>
    /// 用于前台菜单和用户登录成功
    /// </summary>
    public class MenuDto : MenuBaseDto, ILocalizedMenuDto
    {
        public string DisplayNames { get; set; }
        public bool Checked { get; set; }
        public List<MenuDto> Actions { get; set; }
        public List<MenuDto> Children { get; set; }
    }

    public class MenuSearchPickerDto : MenuBaseDto, ILocalizedMenuDto
    {
        public string DisplayNames { get; set; }
        public int Value => Id;
    }

    #region Input

    public class BaseInputMenu
    {
        [DataType(DataType.Text)]
        public string DisplayName { get; set; }

        [DataType(DataType.Text)]
        public string Description { get; set; }

        [DataType(DataType.Text)]
        public string Icon { get; set; }

        [DataType(DataType.Text)]
        public string Url { get; set; }

        [DataType(DataType.Text)]
        public string Component { get; set; }

        [DataType(DataType.Text)]
        public string ControllerName { get; set; }

        [DataType(DataType.Text)]
        public string ActionName { get; set; }

        [DataType(DataType.Text)]
        public string LinkTarget { get; set; }
    }

    public class SaveMenu : BaseInputMenu
    {
        [Range(0, int.MaxValue)]
        [Required]
        public int Number { get; set; }

        [Required]
        public int ParentId { get; set; }

        public bool IsDisplay { get; set; }

        [Required(ErrorMessage = "The IsAction field is required.")]
        public bool? IsAction { get; set; }
    }

    public class InputRemoveMenu
    {
        public List<int> MenuIds { get; set; }
    }

    public class EditMenu : BaseInputMenu
    {
        // Null/omitted values retain old data. These names explicitly clear nullable strings.
        public List<string> ClearFields { get; set; }

        public int Id { get; set; }

        public int? Number { get; set; }

        public int? ParentId { get; set; }

        public bool? IsDisplay { get; set; }

        public bool? IsAction { get; set; }
    }

    #endregion Input

    #region Query

    public class QueryMenu : QueryBase
    {
        [DataType(DataType.Text)]
        public string DisplayName { get; set; }

        public bool? IsAction { get; set; }

        public bool IsDisplay { get; set; }

        public int? ParentId { get; set; }
    }

    #endregion Query

    public class BaseRoleModel
    {
        [Required]
        public Guid RoleId { get; set; }
    }

    public class RoleMenuActionModel : BaseRoleModel
    {
        [Required]
        public List<MenuAction> Menus { get; set; }
    }

    public class MenuAction
    {
        public int MenuId { get; set; }
        public List<int> ActionIds { get; set; }
    }

    public class MenuModel
    {
        public int Id { get; set; }

        public string DisplayName { get; set; }

        public string Label { get; set; }

        public int Number { get; set; }

        public string Url { get; set; }

        public string LinkTarget { get; set; }

        public int ParentId { get; set; }

        public bool IsDisplay { get; set; }

        public string Icon { get; set; }

        public DateTime? UpdatedTime { get; set; }

        public virtual List<MenuModel> Children { get; set; }
    }

    public class MenuSortModel
    {
        [Range(1, int.MaxValue)]
        public int CurrentId { get; set; }

        public int PrevId { get; set; }
        public int NextId { get; set; }
    }
}
