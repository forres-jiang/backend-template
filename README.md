# Backend Template

## Architecture

The application owns its ports; persistence and external adapters implement them. Numbered project filenames control solution display order, not dependency direction.

```text
APIs -> Service -> Contracts -> Shared
APIs -> Persistence -> Service / Contracts / Shared
APIs -> Infrastructure -> Service
```

| Project | Responsibility |
| --- | --- |
| APIs | HTTP endpoints, response envelopes, JWT/request adapters, authorization, localization and composition root |
| Service | Use cases, business policies, repository ports, application DTO mapping; no persistence/ORM/Redis implementation references |
| Persistence | Repository implementations, database entities, entity mappings, SQL, transactions and database registration |
| Contracts | Application inputs/read DTOs and provider-independent batch results |
| Infrastructure | Redis permission cache, external HTTP clients, Excel export and adapter registration |
| Shared | Shared primitives, configuration and business errors |

`Service/Ports` owns repository interfaces. Their signatures and model properties contain no storage entities. `PersistenceMapper` maps database entities inside Persistence; `ApplicationMapper` only maps application models. Mail queue flags and storage defaults are adapter details.

`MenuCommandService`, `MenuQueryService` and `RolePermissionService` separate maintenance, presentation queries and authorization changes. `MenuService` is a compatibility facade retaining the existing controller contracts. `MenuOrder`, `MenuHierarchy`, `MenuTree` and `MenuDisplayNames` hold testable policies independent of database execution.

APIs calls `AddBusinessServices`, `AddRepositories`, `AddPersistenceDatabases`, `AddExternalAdapters` and `AddPermissionCaching`. Registrations are explicit and owned by their project; framework request/JWT adapters remain at the composition root. No assembly scanning is needed for these services. Existing marker interfaces are retained for source compatibility.

DTO namespaces remain `My.XXX.Service.DTOs` for compatibility, although the types live in Contracts. API response classes retain the `My.XXX.Shared` namespace and live in APIs. New storage adapters must depend on application ports, not move storage models into Contracts.

Architecture tests enforce project directions, application assembly dependencies, recursive port model boundaries, materialized repository results and active controller method bodies. The `[NonController]` demo is an example and is excluded from active endpoint rules.

## Transactions and permission consistency

All menu and role-menu mutations first update the singleton `PermissionRevision` row inside their transaction. This serializes administrative writers across processes before they read and calculate their changes. Role replacements, parent validation, localized-name changes and ordering therefore use a snapshot protected by the same write lock. The data and revision commit together; unsuccessful operations and exceptions roll both back. An active `(RoleId, MenuId)` unique index additionally prevents duplicate grants.

The global revision intentionally favors simple correctness for low-volume administrative writes. It serializes unrelated menu/role edits and invalidates all permission projections when any menu changes. If this becomes a measured bottleneck, migrate to per-role revisions and a separate menu-structure lock.

Authorization in Redis mode reads the revision from the primary business database, then addresses a cache key containing the configured prefix, revision and a hash of the user ID plus canonical role IDs. It rechecks the revision after reading cached/database permissions and retries if it changed. A slow writer can only populate an obsolete versioned key, which subsequent requests cannot use. After three concurrent changes it falls back to a direct permission query. Requests already in progress may complete against the state read during that request; this is not cancellation of in-flight requests.

This removes the database-commit/Redis-delete failure window without a distributed transaction or Outbox. Every cache hit still costs two small primary-database revision reads; the cached projection avoids the role/menu join. Redis and database failures propagate rather than granting access from unchecked stale data. Do not route revision reads to lagging replicas. Every writer, including maintenance SQL, must take the same row lock and increment the revision in its data transaction. Old application instances do not follow this protocol and must not run alongside the upgraded version.

Database mode reads permission paths directly and does not use Redis. Menu writes still require the revision table in both modes. Permission membership still comes from authenticated role claims; changing user role claims or revoking issued JWTs is a separate concern. Logout clears current versioned and legacy cache keys; it does not implement token revocation.

Other multi-row operations use `AtomicWrite`, which commits successful results, rolls back unsuccessful results and propagates unexpected exceptions. Avoid composing independently committing repository methods for one atomic use case.

## Database upgrade before deployment

1. Stop old instances that can write menus or role assignments.
2. Back up the business database and check for duplicate active role-menu assignments. The unique index deliberately fails if duplicates exist; resolve them according to the intended assignments instead of silently deleting data.
3. Run the matching script against **Default** (not MailMaster):
   - `My.XXX.Persistence/Migrations/001_permission_revision.sqlserver.sql`
   - `My.XXX.Persistence/Migrations/001_permission_revision.postgresql.sql`
4. Configure a distinct `PermissionCache:KeyPrefix` per application/environment sharing Redis, deploy all upgraded instances, and check readiness including `permission-schema`.

Both upgrade scripts are transactional and repeatable. They create a singleton version table and a filtered/partial unique index; they do not create the existing business tables or auto-run at startup. After a database restore or rollback to older writers, change the cache prefix before resuming the versioned protocol so historical revision numbers cannot reuse old Redis entries.

## API compatibility and partial updates

Application commands return FluentResults. Controllers retain their existing response envelopes and boolean command projections. Validation and unexpected-exception envelopes remain distinct for wire compatibility; response filters reject unconverted FluentResults failures.

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

The business database (`Default`) and mail database (`MailMaster`) independently support SQL Server (2016+) or PostgreSQL (13+). Existing installations default to SQL Server. Set each provider to `SqlServer` or `PostgreSQL` (case insensitive); unknown values fail at startup.

For PostgreSQL, supply these environment variables through your deployment/secret provider:

```text
DatabaseProviders__Default=PostgreSQL
DatabaseProviders__MailMaster=PostgreSQL
ConnectionStrings__Default=Host=localhost;Port=5432;Database=xxx;Username=xxx;Password=<secret>
ConnectionStrings__MailMaster=Host=localhost;Port=5432;Database=mailmaster;Username=xxx;Password=<secret>
```

For SQL Server, use `SqlServer` and a connection string such as `Server=localhost;Database=xxx;User Id=xxx;Password=<secret>;Encrypt=true`. Mixed deployments are supported, for example PostgreSQL for `Default` and SQL Server for `MailMaster`. Both database readiness checks use the selected driver. Existing `enc:v1:` encrypted connection strings remain supported.

Provision tables before running the application; it does not automatically create or migrate databases. PostgreSQL tables use `public` (SQL Server mappings retain `dbo`), with the exact table and column casing declared in the entities: for example `public."Menus"`, `"Id"`, and `public."MAILQUEUE"`. Tables without an explicit schema use the connection's default schema/search path. Use quoted identifiers when creating PostgreSQL tables, identity columns for generated integer keys, `uuid` for GUIDs, `boolean` for booleans, `bytea` for attachments, and `timestamp without time zone` for the existing wall-clock `DateTime` fields. Existing SQL Server data and stored procedures require a separate migration. `DemoRepository.QueryProcMultiple` is a SQL Server-only example requiring a custom `TEST` procedure; it explicitly rejects PostgreSQL.
