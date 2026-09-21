using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using My.XXX.APIs.Common;
using My.XXX.APIs.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace My.XXX.IntegrationTests;

// Loaded only into the test host. This assembly is never an application dependency.
[ApiController]
[AllowAnonymous]
[Route("__test/boundary")]
public sealed class BoundaryProbeController : ControllerBase
{
    [HttpGet("failure")]
    public MyResult Failure() => Result.Fail("Save failed.").ToApiResult();

    [HttpGet("success")]
    public IActionResult Success() => Ok(new { Id = 7 });

    [HttpPost("validate")]
    public MyResult Validate(ProbeInput input) => MyResult.Success();

    [HttpGet("exception")]
    public MyResult Exception() => throw new InvalidOperationException("private database detail");

    [HttpGet("raw")]
    [NonUnifyResult]
    public IActionResult Raw() => Ok(new { Id = 7 });
}

public sealed class ProbeInput
{
    [Required]
    public string Name { get; set; }
}
