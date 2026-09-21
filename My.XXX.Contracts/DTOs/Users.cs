using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace My.XXX.Contracts.DTOs
{
    public class LoginModel
    {
        [Required]
        public string Ticket { get; set; }
    }

    public class TokenModel
    {
        [Required]
        public string RefreshToken { get; set; }
    }

    public class UserData
    {
        public List<Guid> RoleIds { get; set; } = new List<Guid>();
    }

    public class UserInfo
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public List<string> Roles { get; set; }
        public List<Guid> RoleIds { get; set; }
        public List<MenuDto> Menus { get; set; }
        public string Email { get; set; }
    }

    public class LoginUser
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public List<string> Roles { get; set; }
        public List<MenuDto> Menus { get; set; }
    }

    public class Users
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string EmailAddress { get; set; }
        public string MobileNo { get; set; }
        public string ActiveFlag { get; set; }
        public string UserType { get; set; }
        public List<ApplicationRole> Roles = new();
        public List<ApplicationMenu> Menus = new();
    }

    public class ApplicationRole
    {
        public Guid RoleID { get; set; }

        public string ApplicationCode { get; set; }

        public string RoleName { get; set; }

        public string ActiveFlag { get; set; }

        public string RoleType { get; set; }
    }

    public class ApplicationMenu
    {
        public Guid MenuID { get; set; }

        public string ApplicationCode { get; set; }

        public string MenuName { get; set; }

        public Guid? ParentMenuID { get; set; }

        public string MenuURL { get; set; }

        public string MenuTarget { get; set; }

        public string ActiveFlag { get; set; }

        public int MenuOrder { get; set; }
    }
}