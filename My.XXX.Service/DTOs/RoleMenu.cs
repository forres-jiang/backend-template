using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace My.XXX.Service.DTOs
{
    public class RoleMenuBase
    {
        [Required]
        public Guid RoleId { get; set; }
    }

    public class InputRoleMenuBase
    {
        [Required]
        public Guid RoleId { get; set; }

        [Required]
        public bool? Checked { get; set; }
    }

    public class InputRoleMenu : InputRoleMenuBase
    {
        [Range(1, int.MaxValue)]
        [Required]
        public int MenuId { get; set; }
    }

    public class InputRoleMenus : InputRoleMenuBase
    {
        [Required]
        public List<int> MenuIds { get; set; }
    }

    public class RoleMenuIds : RoleMenuBase
    {
        [Required]
        public List<int> MenuIds { get; set; }
    }

    public class RemoveRoleMenu : RoleMenuBase
    {
        [Required]
        public int MenuId { get; set; }
    }

    #region RoleMenuQuery

    public class RoleMenuQuery
    {
        /// <summary>
        /// 多个角色ID
        /// </summary>
        public List<Guid> RoleIds { get; set; }
    }

    #endregion RoleMenuQuery
}