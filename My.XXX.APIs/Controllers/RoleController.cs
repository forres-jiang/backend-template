using Microsoft.AspNetCore.Mvc;
using My.XXX.Service.DTOs;
using My.XXX.Service.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace My.XXX.APIs.Controllers
{
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize("Permissions")]
    [Route("api/[controller]")]
    public class RoleController : ControllerBase
    {
        private readonly IAppCenterService _appCenterService;

        public RoleController(IAppCenterService appCenterService)
        {
            _appCenterService = appCenterService;
        }

        [HttpPost]
        [Route("List")]
        public async Task<List<Role>> GetRoles()
        {
            var result = await _appCenterService.GetRole();
            return result;
        }
    }
}