# Backend Template

## Architecture

The solution uses traditional layered architecture with explicit application and infrastructure boundaries:

Project display names and project files are numbered by architectural responsibility: `01 APIs`, `02 Service`, `03 Persistence`, `04 Contracts`, `05 Shared`, `06 Infrastructure`. Infrastructure adapters are listed after the application and shared projects. These numbers control display order, not dependency direction; the dependency graph below remains authoritative. `07 Tests` is a separate verification project. Directory names, assembly names and namespaces do not include these ordering prefixes.

```text
APIs -> Service -> Persistence -> Contracts -> Shared
APIs -> Infrastructure -> Service
Service -> Contracts / Shared
Persistence -> Shared
```

| Project | Responsibility |
| --- | --- |
| APIs | HTTP endpoints, response DTOs, localization, JWT adapter, authentication/authorization and dependency composition |
| Service | Application use cases, business validation, mapping, application ports; no SQL execution |
| Persistence | Repository interfaces and implementations, persistence models, SQL queries, transaction execution |
| Contracts | Application DTOs shared with persistence and external adapters |
| Infrastructure | External HTTP clients, Redis permission cache and Excel export |
| Shared | Small shared types, configuration objects, business errors and paging; no web, database or Redis packages |

DTO namespaces remain `My.XXX.Service.DTOs` for source compatibility. Shared types use `My.XXX.Shared` (renamed from the former Infra project). Response types also use the `My.XXX.Shared` namespace but belong to the APIs assembly; DTOs belong to Contracts. This is intentionally a traditional layered design: Service still references Persistence for repositories and internal persistence mappings. It is not a domain-independent Clean Architecture model.

Rules enforced by architecture tests:

- Service code must not reference ASP.NET request types.
- Service code must not call the static Redis client; caching is accessed through `IPermissionCache`.
- HTTP claims and culture are exposed to Service through `ICurrentRequest`.
- Database operations belong behind repository interfaces; new Service code must not introduce direct contexts.
- Application interfaces must not expose persistence models, ORM result types or API response envelopes.
- Repository interfaces return materialized results, not `IQueryable`; paged queries execute in Persistence.
- Controllers must not depend on database contexts or repositories.
- Project references follow the allowed dependency graph; Service must not reference ORM, Redis, Excel or infrastructure implementations in its compiled assembly.

`My.XXX.APIs` is the composition root. Framework-specific implementations are registered there.

## Use cases and transactions

`AuthenticationService` coordinates login, role mapping, refresh and logout through `IAppCenterService`, `ITokenIssuer`, `ICurrentRequest` and `IPermissionCache`. The controller maps the session to the existing login response, including top-level token fields. JWT mechanics stay in APIs. `PermissionQuery` owns permission-path lookup and caching, independently of menu maintenance.

Application commands return FluentResults. Ordinary DTO queries remain ordinary queries. API controllers explicitly call `ToApiResult` or `ToLoginResult`; existing boolean command endpoints project `IsSuccess` to preserve their wire contract. `BatchWriteSummary` preserves the former batch response fields without exposing an ORM type.

Application services choose an atomic repository operation. Persistence executes its SQL through `AtomicWrite`: successful operations commit, unsuccessful outcomes roll back, and unexpected exceptions roll back and propagate to the HTTP exception middleware. Menu ordering, role replacement, demo/detail insertion and mail/attachment insertion are atomic. Do not compose multiple independently committing repository methods when a new use case requires a single transaction: add one atomic operation covering that use case instead.

Both response filters share runtime normalization and preserve HTTP status. Explicitly returning an unconverted FluentResults value is rejected instead of silently wrapping a failure as success. `NonUnifyResult` opts out. Validation and unexpected-exception envelopes retain their existing distinct formats for client compatibility.

## Verification

```sh
dotnet test MyXXXSolution.sln
dotnet publish My.XXX.APIs/01My.XXX.APIs.csproj -c Release
```

Tests cover dependency rules, transaction commit/rollback through an instrumented ADO.NET connection, login orchestration, DTO serialization, DI resolution and real HTTP success, failure, validation, authorization and exception paths. SQL Server write semantics and rollback against a real database still require integration validation in an environment with disposable databases. No database migrations are introduced by this refactor.
