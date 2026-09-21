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
[Route("api/v2/permissions")]
public sealed class PermissionController(IPermissionAdministration permissions) : ControllerBase
{
    [HttpGet("catalog")]
    [RequiresPermission(PermissionCodes.PermissionsRead)]
    public MyResult<IReadOnlyList<string>> Catalog() => MyResult<IReadOnlyList<string>>.Success(PermissionCodes.All);

    [HttpGet("roles/{roleId:guid}")]
    [RequiresPermission(PermissionCodes.PermissionsRead)]
    public async Task<MyResult<List<string>>> Get(Guid roleId) =>
        MyResult<List<string>>.Success(await permissions.GetAsync(roleId, HttpContext.RequestAborted));

    [HttpPut("roles/{roleId:guid}")]
    [RequiresPermission(PermissionCodes.PermissionsWrite)]
    public async Task<ActionResult<MyResult<bool>>> Replace(Guid roleId, [FromBody] List<string> codes)
    {
        var result = await permissions.ReplaceAsync(roleId, codes, HttpContext.RequestAborted);
        return result.ToHttpResult();
    }
}
