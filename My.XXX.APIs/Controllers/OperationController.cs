using Microsoft.AspNetCore.Mvc;
using My.XXX.APIs.Models;
using My.XXX.Contracts.DTOs;
using My.XXX.Services.Operations.Interfaces;
using My.XXX.Shared;
using System.Threading.Tasks;

namespace My.XXX.APIs.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize("Permissions")]
    [Route("api/[controller]")]
    [ApiController]
    public class OperationController : ControllerBase
    {
        private readonly IOperationService _requestLogService;

        public OperationController(IOperationService requestLogService)
        {
            _requestLogService = requestLogService;
        }

        [HttpPost]
        [Route("list")]
        [My.XXX.APIs.Common.RequiresPermission(My.XXX.Services.Authorization.Policies.PermissionCodes.OperationList)]
        public async Task<MyResult<Paged<OperationDto>>> List(OperationQeury query)
        {
            return MyResult<Paged<OperationDto>>.Success(await _requestLogService.GetRequestLogs(query));
        }
    }
}
