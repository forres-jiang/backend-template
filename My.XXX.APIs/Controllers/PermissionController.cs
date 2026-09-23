using Microsoft.AspNetCore.Mvc;
using My.XXX.APIs.Common;
using My.XXX.APIs.Models;
using My.XXX.Services.Authorization.Interfaces;
using My.XXX.Services.Authorization.Policies;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace My.XXX.APIs.Controllers;

[ApiController]
[ExplicitApiContract]
[Route("api/v2/permissions")]
public sealed class PermissionController(IPermissionAdministration permissions) : ControllerBase
{
    [HttpGet("catalog")]
    [RequiresPermission(PermissionCodes.PermissionsRead)]
    public ApiResponse<IReadOnlyList<string>> Catalog() => new(1, "Success", PermissionCodes.All, traceId: HttpContext.TraceIdentifier);

    [HttpGet("roles/{roleId:guid}")]
    [RequiresPermission(PermissionCodes.PermissionsRead)]
    public async Task<ApiResponse<List<string>>> Get(Guid roleId) =>
        new(1, "Success", await permissions.GetAsync(roleId, HttpContext.RequestAborted), traceId: HttpContext.TraceIdentifier);

    [HttpPut("roles/{roleId:guid}")]
    [RequiresPermission(PermissionCodes.PermissionsWrite)]
    public async Task<ActionResult<ApiResponse<bool>>> Replace(Guid roleId, [FromBody] List<string> codes)
    {
        var result = await permissions.ReplaceAsync(roleId, codes, HttpContext.RequestAborted);
        return result.ToHttpResult(HttpContext.TraceIdentifier);
    }
}
