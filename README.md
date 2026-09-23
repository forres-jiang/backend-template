# Backend Template

## Architecture

The application owns its ports; persistence and external adapters implement them. Numbered project filenames control solution display order, not dependency direction.

```text
APIs -> Services -> Contracts / Shared
APIs -> Persistences -> Services / Contracts / Shared
APIs -> Infrastructure -> Services
```

| Project | Responsibility |
| --- | --- |
| APIs | HTTP endpoints, response envelopes, JWT/request adapters, authorization, localization and composition root |
| Services | Use cases, menu state and policies, transaction/read ports, application DTO mapping; no persistence/ORM/Redis implementation references |
| Persistences | Repository implementations, database entities, entity mappings, SQL, transactions and database registration |
| Contracts | Application inputs/read DTOs and provider-independent batch results |
| Infrastructure | Versioned permission cache, operational log sinks, external HTTP clients, Excel export and adapter registration |
| Shared | Shared primitives and business errors |

Services is organized by feature: `Menus`, `Authentication`, `Authorization`, `Operations` and `Examples`. Each feature owns its interfaces, ports, models and policies; `Abstractions/Interfaces` contains the shared current-user/culture context. Namespaces follow these feature directories. Repository port signatures and model properties contain no storage entities. Menu reads, permissions and transactional writes use separate `IMenuReadRepository`, `IPermissionStore` and `IAccessControlTransaction` ports. Menu persistence maps entities to `Services/Menus/Models/MenuState`; only `ApplicationMapper` creates menu response DTOs. `MenuSearch` carries normalized query criteria without reusing HTTP-bound pagination inputs.

`MenuCommandService` and `MenuQueryService` own menu maintenance and presentation queries. In Authorization, `RoleMenuAssignmentService` manages role/menu assignments while `PermissionAdministration` manages stable permission codes. `Compatibility/MenuService` remains a compatibility facade retaining the existing controller contracts. New callers use the focused menu query/command services and role assignment service directly. Menus and Authorization do not directly reference one another; their coordination port lives in `AccessControl/Ports`. Role assignment reads only menu IDs through that port. `MenuMutations` owns hierarchy checks, partial updates and ordering; `RoleMenuMutations` owns role-selection changes. Both run state-dependent rules through the existing `IAccessControlTransaction.Execute`; the adapter locks the revision before exposing `IAccessControlWriteSession` and invalidates that session after the transaction. The transaction remains shared because menu changes and role/menu assignments must serialize against the same revision. Failed results and exceptions roll back data and revision together. Compose persistence primitives inside one session; do not nest transactions or compose independently committing use cases when atomicity is required.

Menu and role services depend only on `ICurrentUser` and/or `ICurrentCulture`; authentication uses `IAuthenticationSession`. `UserService` also uses `IAuthenticationSession`; the API `HttpCurrentRequest` adapter implements the narrow context interfaces and keeps claim parsing inside APIs. The unused `IUserService.GetContextUser()` method and `ICurrentRequest` compatibility interface have been removed. `TimeProvider` supplies menu audit timestamps and can be replaced in tests.

APIs calls `AddBusinessServices`, `AddRepositories`, `AddPersistenceDatabases`, `AddExternalAdapters` and `AddPermissionCaching`. Registrations are explicit and owned by their project; framework request/JWT adapters remain at the composition root. No assembly scanning is used.

DTOs live in Contracts under the `My.XXX.Contracts.DTOs` namespace. Existing menu JSON fields remain available, but internal menu state no longer inherits from or doubles as a response DTO. API responses and legacy validation types use `My.XXX.APIs.Models`; host/JWT configuration, permission whitelist and host culture/policy constants use `My.XXX.APIs.Configurations`. Redis options and cache mode belong to `Infrastructure/Caching`, logging storage options belong to `Infrastructure/Logging`, and configuration encryption belongs to `Infrastructure/Security`. Shared retains common primitives, utility functions and business errors. Configuration section names, enum numeric values and HTTP JSON contracts are unchanged; C# consumers must update imports for the moved types. New storage adapters must depend on application ports, not move storage models into Contracts.

Database connectivity and schema health checks live in `Persistences/Health`, exposed through `AddPersistenceHealthChecks`. Redis health checks live in `Infrastructure/Health`, exposed through `AddRedisHealthChecks`. APIs selects and registers the adapters and exposes the health routes. Readiness includes `database`, `schema-version`, `permission-schema`, `authentication-schema`, and optionally `redis`, with the `ready` tag and five-second timeouts. `schema-version` verifies that every migration embedded in this application has a matching name and checksum in `SchemaMigrations`; connectivity alone is not enough. Additional migration records are allowed for additive rolling upgrades, but this is not proof that a destructive future migration is compatible. Redis readiness is registered only when a Redis connection is configured. `/healthy` remains a process liveness check; `/ready` includes these dependency checks.

Architecture tests enforce project directions, application assembly dependencies, recursive port model boundaries, materialized repository results, narrow contexts, typed telemetry, feature dependencies, and HTTP entry points (controllers, JWT adapters, filters and exception middleware). Policies/models cannot reference JSON serializers, and storage ports cannot accept HTTP pagination models. Menu ports and persistence mappings must not expose menu wire models, and menu endpoints must not expose internal state. The `[NonController]` demo is an example and is excluded from active endpoint rules.

## Application boundaries and compatibility

`ISessionService` owns session validation and issuance. `AuthenticationService` reuses validation before its atomic refresh-token rotation; the conditional rotation remains the concurrency guard. JWT adapters only validate token cryptography/purpose, translate claims, and encode tokens. Session lifetime binds `SessionOptions` from the existing `JwtConfig` section. Trusted identity integrations call `ISessionService.IssueAsync`; `ITokenIssuer` now only encodes a token pair. Authentication failures have stable `Authentication.InvalidToken` and `Authentication.RefreshRejected` codes.

`QueryBase` preserves raw one-based HTTP input. `PageWindow.FromRequest` performs normalization at application entry points (nonpositive page becomes the first page; size is clamped to 1–100). Menu and operation repositories receive `MenuSearch` and `OperationSearch`; the latter contains explicit offset/limit. Existing HTTP paging behavior is preserved, but C# consumers must no longer expect `QueryBase.PageIndex` to decrement itself. Simple read DTOs and telemetry records remain shared intentionally.

`MenuState.DisplayNames` is immutable `LocalizedText`. Business policies operate on culture/name values; persistence and application mappings use the legacy JSON codec in `Contracts/Serialization`. Valid translations retain their values; malformed legacy JSON normalizes to an empty translation set and still falls back to `DisplayName`. Updating a copy does not mutate the original translations. The database column and HTTP field remain strings; no schema migration is required.

`MenuQueryService.FindAsync` returns `Result<MenuBaseDto>` with `Menu.NotFound` for a missing menu. The legacy facade translates this to its existing null result; existing endpoints keep their response/status contracts. New endpoints use `ToHttpResult` (including the generic overload) for explicit status mapping. The existing response filter remains for legacy compatibility.

Internal C# migration points: import `Compatibility` for the legacy facade, use `AccessControl.Ports.IAccessControlTransaction` / `IAccessControlWriteSession`, update session issuance callers, and convert menu translation strings at mapping boundaries. No route or configuration key changes are required.

### Compatibility exit policy

The existing `MenuController` is the only grandfathered application caller of `IMenuService`. Architecture tests enforce this exact allowlist (plus the facade implementation and its DI registration), including dependencies inside async state machines and generic types. New callers use `MenuCommandService`, `MenuQueryService`, and `RoleMenuAssignmentService`. Do not expand the allowlist to introduce new features.

| Existing contract | Migration target | Removal condition |
| --- | --- | --- |
| `/api/Menu` menu CRUD, search and tree routes | Focused menu command/query services and an explicitly versioned API | Consumers have migrated; legacy wire-contract tests pass until retirement |
| `/api/Menu` role-menu assignment routes | `RoleMenuAssignmentService`; `RoleAccessAdministration` when menu selection and permission codes must commit together | Consumers have migrated to the replacement contract |
| `MyResult`, boolean projections and `UnifyResultAsync` | `[ExplicitApiContract]`, `ApiResponse<T>` and `ToHttpResult` | No legacy endpoint depends on the old projection/filter |

This change adds no routes and does not remove or change existing HTTP contracts. `/api/v2/permissions` already uses the explicit contract. A future combined HTTP endpoint must authorize both kinds of administration before calling `RoleAccessAdministration`; the application use case does not authorize HTTP requests. Retire compatibility code only in an announced breaking release after endpoint usage confirms migration. `ResultMigrationTests`, `MenuQueryBoundaryTests` and HTTP integration tests retain the old response semantics during that period.

`BusinessError.Code` identifies the specific failure; `BusinessError.Kind` classifies it as validation, not-found, conflict, unauthorized or forbidden. APIs maps the category to 400/404/409/401/403. New codes therefore require no changes to the shared HTTP mapper. Errors default to validation; producers must explicitly choose other categories. Legacy `StatusCode` remains an envelope value and is independent of this category. Never serialize a raw `BusinessError` as a response.

## Operational logging

`ExceptionHandlingMiddleware` is the sole request-log collector; the old `AppMetricsAsync` filter is removed. It records metadata only and respects `IgnoreMetrics` for ordinary requests while still recording failures. `IRequestLogWriter` delegates sink selection and failure isolation to Infrastructure, using the existing `AppConfig:RequestLogStorageType` and `ExceptionStorageType` keys. A failed SQL sink does not replace the HTTP outcome or prevent the text sink in `TextAndSQL` mode. Legacy email modes fall back to text; the removed mail module is not restored.

`MetricsInfo` accepts serialized strings instead of arbitrary objects for legacy payload columns; persistence does not double-encode them. The HTTP collector never captures payloads. `QueuedRequestLogWriter` enqueues metadata into a bounded in-process channel; requests do not wait for SQL/text sinks. A hosted worker creates an independent DI scope per record and applies the configured write timeout. Full or closed queues reject new records and increment `request_logs.dropped`. Shutdown drains the queue until the host's shutdown deadline. Crashes, sink errors and forced shutdown can lose records: this is best-effort telemetry, not durable business audit. Queue capacity and write timeout bind from `AppConfig:QueueCapacity` and `AppConfig:WriteTimeoutSeconds`.

## Transactions and permission consistency

`AddPermissionCaching` selects the direct `PermissionQuery` or the Infrastructure `CachedPermissionQuery` at composition time. Cache keys, TTL and revision retry mechanics live entirely in Infrastructure; application services do not switch on Redis configuration. Registration works before or after business services. Restart to change cache mode.

All menu, role-menu and role-permission mutations first update the singleton `PermissionRevision` row inside their transaction. This serializes administrative writers across processes before they read and calculate their changes. Role replacements, parent validation, localized-name changes and ordering therefore use a snapshot protected by the same write lock. The data and revision commit together; unsuccessful operations and exceptions roll both back. An active `(RoleId, MenuId)` unique index additionally prevents duplicate menu grants.

`IRolePermissionStore` is read-only. Stable permission replacements execute through `IAccessControlWriteSession.ReplaceRolePermissions`, using the same transaction adapter as menu writes. `PermissionAdministration` opens a transaction for a standalone replacement. `RoleAccessAdministration.ReplaceAsync` composes `RoleMenuMutations` and `PermissionMutations` using one session, commits once and increments the revision once. If permission validation or persistence fails after changing menu selection, both selections and the revision roll back. Empty lists explicitly clear both selections. Compose session-level mutations inside one `Execute`; never call independently committing services inside another transaction. Existing C# callers of `IRolePermissionStore.ReplaceAsync` must migrate to `IPermissionAdministration.ReplaceAsync` or the combined use case.

The global revision intentionally favors simple correctness for low-volume administrative writes. It serializes unrelated menu/role edits and invalidates all permission projections when any menu changes. If this becomes a measured bottleneck, migrate to per-role revisions and a separate menu-structure lock.

Authorization in Redis mode reads the revision from the primary business database, then addresses a cache key containing the configured prefix, revision and a hash of the user ID plus canonical role IDs. It rechecks the revision after reading cached/database permissions and retries if it changed. A slow writer can only populate an obsolete versioned key, which subsequent requests cannot use. After three concurrent changes it falls back to a direct permission query. Requests already in progress may complete against the state read during that request; this is not cancellation of in-flight requests.

This removes the database-commit/Redis-delete failure window without a distributed transaction or Outbox. Every cache hit still costs two small primary-database revision reads; the cached projection avoids querying and deduplicating role permission codes. Redis and database failures propagate rather than granting access from unchecked stale data. Do not route revision reads to lagging replicas. Every writer, including maintenance SQL, must take the same row lock and increment the revision in its data transaction. Legacy instances that do not follow this protocol must not run alongside the upgraded version.

Database mode reads stable permission codes from `RolePermissions` and does not use Redis. Endpoint metadata declares the required code; menu paths no longer determine authorization. `003_stable_permissions` imports legacy grants once; subsequent menu-selection changes do not implicitly grant API permissions. Menu writes still require the revision table in both modes. Each validated JWT resolves an active session and rebuilds role claims from the current identity store. Logout revokes that session, so subsequent requests using its access or refresh token fail authentication. Requests already in progress are not cancelled.

Other multi-row operations use `AtomicWrite`, which commits successful results, rolls back unsuccessful results and propagates unexpected exceptions. Avoid composing independently committing repository methods for one atomic use case.

## Database upgrade before deployment

1. Stop legacy instances that use the old permission-path model or bypass the revision protocol. Back up the **Default** database. Provision existing business tables first; the upgrade scripts are not a complete empty-database bootstrap.
2. Check duplicate active role-menu assignments and custom action grants. The unique index rejects duplicates, and the stable-permission import rejects unmapped granted actions. Resolve these deliberately before deployment. Do not edit a migration that has already been applied; customize a new installation before its first migration, or add a new migration for existing installations.
3. Use the same published artifact and environment configuration as the application, from its publish directory, to run the explicit deployment command:

   ```sh
   dotnet My.XXX.APIs.dll --migrate
   ```

   For a source checkout, use `dotnet run --project My.XXX.APIs/01My.XXX.APIs.csproj -- --migrate`. Set `ConnectionStrings__Default`, `DatabaseProviders__Default` and `ASPNETCORE_ENVIRONMENT` through the deployment environment. Use a migration identity with DDL rights; the runtime identity needs read access to `SchemaMigrations` but does not need migration rights.
4. The runner applies embedded `001_permission_revision`, `002_authentication` and `003_stable_permissions` scripts for the configured provider in order, under a database session lock. It journals names and SHA-256 checksums in `SchemaMigrations`, skips matching applied scripts and refuses modified ones. A deployment must stop on a nonzero migration exit code.
5. Deploy the application with a distinct `PermissionCache:KeyPrefix` per environment and check `/ready`, including `schema-version`. Normal startup and health checks never run migrations. Installations previously upgraded by manual SQL must run `--migrate` once to populate the journal; the existing scripts are repeatable.

New migrations must have an ordered unique name, be transactional and safely replayable, and include matching SQL Server/PostgreSQL implementations. Each script commits before its journal record is written, so a crash in that window replays the script. Never modify recorded checksums to bypass a mismatch. Additive changes may precede a rolling application upgrade; destructive changes require a separate compatibility review and maintenance/retirement plan. Extra journal rows alone do not make an older application safe to run.

After a database restore or rollback to older writers, change the cache prefix before resuming the versioned protocol so historical revision numbers cannot reuse old Redis entries. Re-run readiness checks against the restored schema before admitting traffic.

## API compatibility and partial updates

Application commands return FluentResults. Controllers retain their existing response envelopes and boolean command projections. Validation and unexpected-exception envelopes remain distinct for wire compatibility; response filters reject unconverted FluentResults failures.

Menu failures carry stable internal `BusinessError.Code` values (`Menu.NotFound`, `Menu.InvalidParent`, `Menu.InvalidInput`, `Menu.InvalidSelection`, `Menu.InvalidOrder`, `Menu.WriteFailed`). Envelope endpoints preserve their specific public messages through the common converter. Legacy boolean endpoints retain their boolean projections; no new error fields or HTTP status changes are introduced.

Menu updates use an explicit allowlist instead of reflection over matching database column names. Omitted/null values retain existing values. To clear a nullable string explicitly, provide `ClearFields`, for example:

```json
{ "Id": 7, "ClearFields": ["Description", "Icon"] }
```

Allowed names are `Description`, `Icon`, `Url`, `Component`, `ControllerName`, `ActionName` and `LinkTarget` (case insensitive). Explicit clearing takes precedence over a supplied value. Invalid names reject the update; identifiers/audit fields cannot be cleared. Parent changes that create cycles, reference missing parents or place children under action nodes are rejected. Empty full role selections now remove all assignments.

## Redis configuration

Configure `RedisConfig__ConnectionString` through environment variables or a secret provider, for example `localhost:6379,defaultDatabase=0,connectTimeout=5000,asyncTimeout=5000`. StackExchange.Redis uses one container-owned multiplexer. Disconnected commands fail promptly instead of queuing stale permission writes. Existing `enc:v1:` encryption remains supported; decrypted strings must use StackExchange.Redis syntax.

Set `AppConfig:PermissionDataCache` to `1` for Redis caching, which requires a Redis connection string. With `0` and no connection string, no Redis connection or Redis readiness check is registered.

```json
"PermissionCache": {
  "KeyPrefix": "your-app:production:permissions:v2",
  "ExpiryInMinutes": 5
}
```

Expiry is independent of JWT lifetime and must be positive. JSON string arrays and the distinction between missing and empty permissions are preserved. Old user-ID-only entries are ignored by authorization and expire naturally; no global Redis flush is needed. Values and expiry are set atomically. Cancellation stops waiting but cannot retract commands already sent. Restart after changing connection/prefix settings.

## Verification

```sh
dotnet test MyXXXSolution.sln
dotnet publish My.XXX.APIs/01My.XXX.APIs.csproj -c Release
```

Unit tests cover architecture boundaries, mapping/serialization, cache revision races, menu policies and transaction outcomes. HTTP integration tests cover host composition and response/authentication/authorization boundaries.

Real database concurrency tests require explicit disposable-server connections with CREATE/DROP DATABASE privileges:

- `ARCH_TEST_POSTGRES`: PostgreSQL administrative connection string.
- `ARCH_TEST_SQLSERVER`: SQL Server administrative connection string.

Each case creates and removes its own randomly named database and runs the matching upgrade script twice. Tests exercise concurrent grants/replacements/sorting, invalid-write rollback, injected database exceptions and explicit field clearing. Unconfigured providers are reported as skipped, not as validated. Do not point these variables at a production server.

## Database providers

The business database (`Default`) supports SQL Server (2016+) or PostgreSQL (13+). Existing installations default to SQL Server. Set the provider to `SqlServer` or `PostgreSQL` (case insensitive); unknown values fail at startup.

For PostgreSQL, supply these environment variables through your deployment/secret provider:

```text
DatabaseProviders__Default=PostgreSQL
ConnectionStrings__Default=Host=localhost;Port=5432;Database=xxx;Username=xxx;Password=<secret>
```

For SQL Server, use `SqlServer` and a connection string such as `Server=localhost;Database=xxx;User Id=xxx;Password=<secret>;Encrypt=true`. The database readiness check uses the selected driver. Existing `enc:v1:` encrypted connection strings remain supported.

Provision tables before running the application; it does not automatically create or migrate databases. PostgreSQL tables use `public` (SQL Server mappings retain `dbo`), with the exact table and column casing declared in the entities: for example `public."Menus"` and `"Id"`. Tables without an explicit schema use the connection's default schema/search path. Use quoted identifiers when creating PostgreSQL tables, identity columns for generated integer keys, `uuid` for GUIDs, `boolean` for booleans, `bytea` for attachments, and `timestamp without time zone` for the existing wall-clock `DateTime` fields. Existing SQL Server data and stored procedures require a separate migration. `DemoRepository.QueryProcMultiple` is a SQL Server-only example requiring a custom `TEST` procedure; it explicitly rejects PostgreSQL.
