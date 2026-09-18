# Backend Template

## Architecture

The solution uses four logical layers. Dependencies point inward through interfaces:

```text
WebApi (HTTP, authentication, dependency composition)
  -> Service/Application (use cases, DTOs, ports)
      -> Data (persistence adapters)
Infra currently contains shared legacy infrastructure and is being reduced over time.
```

Rules enforced by tests:

- Service code must not reference ASP.NET request types.
- Service code must not call the static Redis client; caching is accessed through `IPermissionCache`.
- HTTP claims and culture are exposed to Service through `ICurrentRequest`.
- Database operations belong behind repository interfaces; new Service code must not introduce direct contexts.

`My.XXX.APIs` is the composition root. Framework-specific implementations are registered there.
