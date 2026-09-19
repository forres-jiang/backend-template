using Microsoft.AspNetCore.Mvc;
using My.XXX.Service.DTOs;
using My.XXX.Service.Interfaces;
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
        public async Task<Paged<OperationDto>> List(OperationQeury query)
        {
            return await _requestLogService.GetRequestLogs(query);
        }
    }
}
