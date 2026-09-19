using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace My.XXX.Service.DTOs
{
    public class RolesMenuModel
    {
        [Required]
        public List<Guid> Ids { get; set; }
    }

    public class Role
    {
        public Guid RoleId { get; set; }
        public string RoleName { get; set; }
        public string RoleType { get; set; }
    }
}