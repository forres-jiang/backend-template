# Backend Template 后端开发模板

基于 .NET 10 / ASP.NET Core 的模块化单体后端模板，提供菜单管理、角色菜单分配、稳定权限码授权、JWT 会话、数据库访问、Redis 权限缓存、运行日志及健康检查。

项目采用“应用层定义端口，持久化层和基础设施层实现端口”的设计。适合在同一个部署单元中扩展业务，并通过清晰的依赖边界控制复杂度。

> 本项目是开发模板，需要接入实际身份认证来源、准备业务数据库并配置密钥。仓库没有开箱即用的用户名密码登录流程，也没有完整的空数据库初始化脚本。

## 1. 架构与项目职责

```text
APIs ────────────> Services ────────────> Contracts / Shared
  │                    ↑
  ├──> Persistences ────┘
  │       └───────────────────────────> Contracts / Shared
  └──> Infrastructure ─> Services
```

API 项目负责组装应用。Services 不引用数据库、Redis 或 ASP.NET Core 实现。项目文件名前的数字用于解决方案排序，不表示依赖方向。

| 项目 | 职责 |
| --- | --- |
| `My.XXX.APIs` | HTTP 端点、认证授权适配、响应转换、本地化、配置及依赖注入入口 |
| `My.XXX.Services` | 业务用例、业务规则、应用模型，以及由应用层定义的存储和事务端口 |
| `My.XXX.Persistences` | LinqToDB 仓储、数据库实体映射、事务、迁移及数据库健康检查 |
| `My.XXX.Contracts` | 应用输入、读取 DTO、批量操作结果及兼容序列化格式 |
| `My.XXX.Infrastructure` | Redis 缓存、HTTP 客户端、日志队列、Excel 导出和配置加密 |
| `My.XXX.Shared` | 通用结果、业务错误及基础工具 |
| `My.XXX.UnitTests` | 业务规则、映射、缓存协议、结果契约及架构约束测试 |
| `My.XXX.IntegrationTests` | HTTP 组合测试，以及 SQL Server / PostgreSQL 真实数据库测试 |

Services 按业务功能组织：

```text
My.XXX.Services/
├── Menus/           菜单命令、查询、层级与排序规则
├── Authorization/   角色菜单分配、权限码查询与维护
├── Authentication/  会话签发、校验、刷新和注销
├── AccessControl/   菜单与授权共享的事务端口
├── Operations/      运行日志查询及写入端口
├── Examples/        示例业务
├── Abstractions/    当前用户、当前语言等窄接口
└── Compatibility/   旧菜单服务兼容门面
```

## 2. 当前架构的优势

| 设计 | 实际收益 |
| --- | --- |
| 应用层拥有端口 | 业务规则不绑定 ORM、缓存或 HTTP，可用内存替身测试，并在适配器中更换技术实现 |
| 按功能组织代码 | 菜单、认证、授权的接口、规则与模型集中维护，减少跨目录查找 |
| 菜单命令与查询分离 | 更新规则和展示逻辑各自演进；内部 `MenuState` 不直接作为 HTTP 响应 |
| 共享访问控制事务 | 角色菜单与权限码可一起提交；后一步失败时，前一步写入与修订号一起回滚 |
| 稳定权限码 | 权限标识不依赖菜单文字、URL 或控制器名称，导航调整不会自动改变 API 授权 |
| 有状态会话验证 | JWT 签名验证后继续检查会话与当前用户；注销、禁用用户和角色调整能影响后续请求 |
| 版本化权限缓存 | 数据与修订号同事务提交，避免提交成功但缓存删除失败造成的旧权限复用 |
| 显式错误分类与兼容边界 | 新接口按错误类别映射 HTTP 状态；旧接口保留原有响应，支持渐进迁移 |
| 迁移记录与就绪检查 | 部署校验迁移名称和校验和，区分数据库可连接与满足当前应用要求 |
| 架构测试 | 自动检查依赖方向、端口模型、控制器边界及兼容门面调用者，减少结构退化 |

这些设计也有明确取舍：管理写操作使用全局权限修订锁；Redis 命中仍需读取数据库修订号；运行日志使用可能丢失记录的内存队列。当前实现优先保证管理场景的一致性和可维护性。

## 3. 本地启动

### 3.1 环境准备

- 安装符合 `global.json` 的 .NET 10 SDK：基准版本为 `10.0.100`，允许同主次版本内更新的 feature band。
- 准备 SQL Server 或 PostgreSQL 数据库。默认提供程序为 `SqlServer`。
- 根据 `My.XXX.Persistences/PersistentObjects` 中的实体映射准备业务表。现有迁移会创建权限修订、认证会话及稳定权限相关结构，但不会创建全部业务表。
- 默认无需 Redis；只有配置 Redis 时才建立连接并注册对应就绪检查。

从仓库根目录执行以下命令。

### 3.2 配置本地环境

以下为 PowerShell 示例。连接串占位符需要替换为本机开发数据库信息：

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:ASPNETCORE_URLS = 'http://localhost:5000'
$env:DatabaseProviders__Default = 'SqlServer'
$env:ConnectionStrings__Default = 'Server=localhost;Database=xxx;User Id=xxx;Password=<开发数据库密码>;Encrypt=true;TrustServerCertificate=true'

# 仅用于首次本地初始化：生成两份独立随机密钥，不输出密钥内容。
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$encryptionBytes = New-Object byte[] 32
$jwtBytes = New-Object byte[] 32
$rng.GetBytes($encryptionBytes)
$rng.GetBytes($jwtBytes)
$env:APP_ENCRYPTION_KEY = [Convert]::ToBase64String($encryptionBytes)
$env:JwtConfig__Secret = [Convert]::ToBase64String($jwtBytes)
$rng.Dispose()
```

`APP_ENCRYPTION_KEY` 必须是 **32 字节密钥的 Base64 表示**；`JwtConfig:Secret` 至少需要 **32 个 UTF-8 字节**。普通启动会校验两者，即使数据库连接串使用明文，也需要配置应用加密密钥。

上述环境变量仅作用于当前终端及其子进程。持续开发和部署时应保存并复用密钥；随意重新生成会影响已有令牌、加密配置或 Data Protection 密钥的使用。生产环境通过部署平台或密钥管理系统注入，不提交到仓库。

使用 PostgreSQL 时，替换这两项：

```powershell
$env:DatabaseProviders__Default = 'PostgreSQL'
$env:ConnectionStrings__Default = 'Host=localhost;Port=5432;Database=xxx;Username=xxx;Password=<开发数据库密码>'
```

### 3.3 还原、迁移与运行

```powershell
dotnet restore MyXXXSolution.sln

# 业务基础表已准备好后，显式执行升级。
dotnet run --project My.XXX.APIs/01My.XXX.APIs.csproj --no-launch-profile -- --migrate
if ($LASTEXITCODE -ne 0) { throw '数据库迁移失败，请先处理错误。' }

# 正常启动不会自动迁移数据库。
dotnet run --project My.XXX.APIs/01My.XXX.APIs.csproj --no-launch-profile
```

在另一个终端检查：

```powershell
Invoke-WebRequest http://localhost:5000/healthy
Invoke-WebRequest http://localhost:5000/ready
```

- 开发环境 Swagger：`http://localhost:5000/swagger/index.html`。
- `/healthy`：进程存活检查，不检查数据库。
- `/ready`：数据库、迁移记录、认证和权限结构，以及可选的 Redis 检查。
- `DemoController` 标记为 `[NonController]`，其示例方法不是可访问端点。启动配置中的旧 Demo 地址不作为运行验证入口。

## 4. 配置说明

应用在默认配置源之后加载 `Configurations/appsettings.json` 和对应环境的 `appsettings.{Environment}.json`；开发环境随后加载 User Secrets，再由环境变量和命令行参数覆盖。环境配置文件必须存在。环境变量使用双下划线表达层级，例如 `JwtConfig__Secret`。

| 配置项 | 含义 |
| --- | --- |
| `APP_ENCRYPTION_KEY` | 应用加密密钥，直接从环境变量读取，不能只写入 appsettings 或 User Secrets |
| `ConnectionStrings:Default` | 业务数据库连接串，支持明文或已有 `enc:v1:` 加密格式 |
| `DatabaseProviders:Default` | `SqlServer` 或 `PostgreSQL`，忽略大小写；未知值启动失败 |
| `JwtConfig:Secret` / `Issuer` / `Audience` | JWT 签名、签发者和受众配置 |
| `JwtConfig:ExpiryInMinutes` | Access Token 有效期，必须大于零 |
| `JwtConfig:RefreshExpiryInMinutes` | 会话和 Refresh Token 有效期，必须大于 Access Token 有效期 |
| `AppConfig:PermissionDataCache` | `0` 直接查数据库，`1` 使用 Redis 权限缓存 |
| `RedisConfig:ConnectionString` | StackExchange.Redis 格式连接串；启用 Redis 权限缓存时必填 |
| `PermissionCache:KeyPrefix` | 按应用和环境隔离缓存的前缀 |
| `PermissionCache:ExpiryInMinutes` | 权限缓存有效期，必须大于零，与 JWT 有效期独立 |
| `AllowedHostArray` | 允许跨域访问的前端 Origin 数组 |
| `PermissionWhitelist:Codes` | 已认证请求可跳过角色权限查询的权限码列表，按实际需求配置 |
| `DataProtection:ApplicationName` / `KeyPath` | Data Protection 应用隔离名称和密钥持久化目录 |
| `AppConfig:EnableRequestLog` | 是否收集普通请求日志 |
| `AppConfig:RequestLogStorageType` / `ExceptionStorageType` | 普通请求及异常日志的存储模式 |
| `AppConfig:QueueCapacity` | 运行日志队列容量，默认 `1024`，必须大于零 |
| `AppConfig:WriteTimeoutSeconds` | 单条日志写入超时，默认 `5` 秒，必须大于零 |

启用 Redis 的示例：

```powershell
$env:AppConfig__PermissionDataCache = '1'
$env:RedisConfig__ConnectionString = 'localhost:6379,defaultDatabase=0,connectTimeout=5000,asyncTimeout=5000'
$env:PermissionCache__KeyPrefix = 'my-app:development:permissions:v3'
$env:PermissionCache__ExpiryInMinutes = '5'
```

缓存模式在依赖注入组装时选择，修改后需重启。每个容器复用一个 Redis multiplexer。即使缓存模式为 `0`，只要配置了 Redis 连接串，仍会注册 Redis 连接和就绪检查。

PostgreSQL 表名、列名以实体映射为准，注意带双引号的大小写标识符，例如 `public."Menus"`。已有 SQL Server 数据和自定义存储过程需要单独迁移；`DemoRepository.QueryProcMultiple` 是仅支持 SQL Server 的示例。

## 5. 身份认证与接口调用

### 5.1 接入身份来源

应用没有默认登录账号。接入企业 SSO、第三方身份服务或自建登录流程时，应先验证用户身份，再在受信任的服务端流程中：

1. 通过 `IAuthenticationStore.SetUserAsync` 保存应用层 `UserIdentity` 身份快照，或实现适配自身身份系统的存储端口。`UserIdentity` 不包含菜单，角色集合会防御性复制并以只读方式暴露；身份变化使用新的快照（例如 `identity with { Roles = new[] { "Reader" } }`）替换。
2. 通过 `ISessionService.IssueAsync(userId)` 创建持久化会话并签发令牌对。
3. 将 Access Token 和 Refresh Token 返回给已验证身份的调用方。

`ICurrentUser.User`、认证存储和 `ITokenIssuer.Create` 使用 `UserIdentity`；旧 HTTP 响应仍使用 `UserInfo`，由 `UserService` 显式映射，修改响应集合不会修改当前身份。自定义认证适配器需同步更新这些 C# 接口；数据库身份快照格式和现有 HTTP 字段保持兼容。

`ITokenIssuer` 只负责编码令牌；直接调用它不会创建可验证的持久化会话。不能把任意传入的用户 ID 当作可信登录依据。

### 5.2 调用现有接口

业务请求使用 `Authorization: Bearer <Access Token>`；刷新接口使用 Refresh Token：

```http
GET /api/User/GetRoles
Authorization: Bearer <Access Token>

POST /api/User/RefreshToken
Authorization: Bearer <Refresh Token>

DELETE /api/User/Session
Authorization: Bearer <Access Token>
```

每次 JWT 验证都会校验有效会话，并从当前身份快照重建角色信息。刷新采用条件更新轮换 Refresh Token 标识，并发刷新只有一个请求可以成功。注销后，该会话的后续 Access / Refresh Token 请求无法通过认证；已在处理中的请求不会被撤销。

## 6. 菜单与权限管理

### 6.1 导航和授权分别维护

- 菜单及 `RoleMenu` 关系决定导航展示和菜单选择。
- `RolePermissions` 中的稳定权限码决定 API 授权。
- 端点通过 `[RequiresPermission(PermissionCodes.MenuAdd)]` 等元数据声明要求。
- 使用权限策略的端点缺少权限码元数据时拒绝授权，即使用户属于 `AppAdmin` 也不会绕过这一检查。
- 权限码元数据有效时，当前身份中的 `AppAdmin` 角色或白名单可跳过普通角色权限查询。

稳定权限迁移只在首次建立权限表时导入旧授权。此后勾选菜单不会自动授予相应 API 权限。

现有权限码接口：

| 请求 | 用途 | 所需权限 |
| --- | --- | --- |
| `GET /api/v2/permissions/catalog` | 查询权限码目录 | `permissions.read` |
| `GET /api/v2/permissions/roles/{roleId}` | 查询角色权限码 | `permissions.read` |
| `PUT /api/v2/permissions/roles/{roleId}` | 完整替换角色权限码，请求体为字符串数组 | `permissions.write` |

替换请求体示例：`["menu.get", "menu.list", "menu.tree"]`。传入空数组会清空该角色权限。新增权限码需要同时加入 `PermissionCodes` 常量及 `All` 目录。

### 6.2 组合事务

单独操作使用 `MenuCommandService`、`RoleMenuAssignmentService` 或 `IPermissionAdministration`。需要同时保存角色菜单和权限码时，在应用用例中调用：

```csharp
// roleAccess 为通过依赖注入获得的 RoleAccessAdministration。
var result = await roleAccess.ReplaceAsync(
    roleId,
    new List<int> { 1, 2 },
    new List<string> { PermissionCodes.MenuGet, PermissionCodes.MenuList },
    cancellationToken);
```

该用例复用一个 `IAccessControlWriteSession`，先更新角色菜单，再替换权限码；只提交一次并只递增一次修订号。后一步校验失败或数据库抛出异常时，两部分数据及修订号一起回滚。两个空列表表示同时清空两种选择。

目前没有暴露对应的组合 HTTP 端点。新增端点时，应校验调用者具备两类管理权限，再调用组合用例。

`MenuReadRepository` 只实现菜单读取，`AccessControlTransaction` 独立管理权限修订锁、数据库事务和写会话生命周期；二者通过 DI 使用请求作用域中的 `DBContext`。

所有公开写用例（`MenuCommandService`、`RoleMenuAssignmentService`、`PermissionAdministration`、`RoleAccessAdministration`）负责开启事务及读取调用上下文。`MenuMutations`、`RoleMenuMutations`、`PermissionMutations` 只接受已有的 `IAccessControlWriteSession`，不再自行提交事务，也不读取当前用户。无状态参数校验可在事务前完成，依赖数据库状态的校验必须留在加锁会话内。

扩展事务时，使用 `IAccessControlTransaction.Execute` 中的会话级操作；不要在事务内调用另一个自行提交的用例，也不要开启嵌套事务。会话在事务结束后失效。`IRolePermissionStore` 只负责读取，写入必须经过共享事务端口。

### 6.3 菜单更新和查询约定

菜单编辑采用显式字段白名单。未传入或值为 `null` 的字段保留原值；清空可空字符串使用 `ClearFields`：

```json
{
  "Id": 7,
  "ClearFields": ["Description", "Icon"]
}
```

允许清空的字段为 `Description`、`Icon`、`Url`、`Component`、`ControllerName`、`ActionName`、`LinkTarget`，忽略大小写。清空优先于同一请求中的赋值；标识、审计字段不能清空。父级调整会校验循环、父节点存在性以及动作节点不能作为父级等规则。

查询输入使用一基页码，应用入口通过 `PageWindow` 规范化：非正页码归到第一页，页大小限制在 `1–100`。仓储接收规范化查询模型，不接收原始 HTTP 分页 DTO。菜单翻译在内部使用不可变 `LocalizedText`，查询在复制的 `MenuState` 上选择语言，通过类型化的 `MenuNode` 组装树和动作节点，最后由 `ApplicationMapper` 输出兼容 DTO 和 JSON 字符串。查询过程不解析响应 JSON，也不修改仓储返回的快照；数据库 JSON 仍由持久化映射器读写。

### 6.4 权限缓存一致性

所有菜单、角色菜单和角色权限写入先在事务内更新单例 `PermissionRevision` 行，获得共享写锁，再读取和修改数据。数据与版本一起提交或回滚。

`IPermissionQuery` 只提供异步权限查询，不暴露同步查询或缓存清理。版本化缓存是实现细节；需要主动清理时使用 Infrastructure 的 `PermissionCacheMaintenance`（配置 Redis 时注册），它会清理当前版本键和迁移期旧键。正常权限变更仍依赖事务修订号保证一致性，无需调用缓存清理；注销仍通过撤销会话生效。

Redis 模式的缓存键包含前缀、修订号及用户和规范化角色集合的摘要。查询先读主库修订号，读取缓存或数据库权限后再次检查修订号；发生变化时重试，连续三次变化后回退到直接查询。慢查询只能写入旧版本键，后续新版本请求不会复用该键。

每次缓存命中仍需两次主库修订号读取。Redis 或数据库故障会传播，不会使用未经版本核对的旧权限继续授权。修订号不能从有延迟的副本读取，维护 SQL 也必须遵守同一事务加锁与版本递增协议。

## 7. 新业务开发与响应契约

新增业务建议按以下顺序实现：

1. 在 Services 的对应功能目录定义应用用例、规则和存储端口。确实属于 HTTP 或数据库的细节留在边界层。
2. 在 Contracts 定义输入和读取 DTO。内部状态模型不继承数据库实体或响应 DTO。
3. 在 Persistences 实现存储端口，返回已物化结果，不向应用暴露 `IQueryable`。
4. 在所属项目的注册方法中显式注册实现，例如 `AddBusinessServices`、`AddRepositories`、`AddExternalAdapters`。
5. 控制器调用应用用例，使用 `HttpContext.RequestAborted` 传递取消信号；需要权限的端点声明稳定权限码。
6. 为关键规则、事务失败路径和 HTTP 契约添加对应测试。

新接口使用 `[ExplicitApiContract]`、`ApiResponse<T>` 和 `ToHttpResult`。业务失败采用 `BusinessError`：

```csharp
var result = Result.Fail<string>(new BusinessError(
    "订单不存在。",
    code: "Order.NotFound",
    kind: BusinessErrorKind.NotFound));

// 在显式契约控制器中转换，保留错误码和请求追踪 ID。
return result.ToHttpResult(HttpContext.TraceIdentifier);
```

`Code` 标识具体错误，`Kind` 决定通用 HTTP 分类：

| 类别 | HTTP 状态 |
| --- | --- |
| `Validation`（默认） | 400 |
| `NotFound` | 404 |
| `Conflict` | 409 |
| `Unauthorized` | 401 |
| `Forbidden` | 403 |

新增业务错误码无需修改公共 HTTP 转换器。旧 `BusinessError.StatusCode` 是历史响应信封中的值，与 HTTP 状态码独立。不要直接把 FluentResults 或内部错误对象作为 HTTP 响应。

### 兼容层退出规则

`MenuController` 是当前唯一保留的 `IMenuService` 应用调用者。架构测试的白名单仅包含该控制器、门面实现、接口本身及服务注册；示例控制器已使用拆分后的服务。新增调用者不得依赖兼容门面。

| 旧契约 | 新调用方式 |
| --- | --- |
| `/api/Menu` 菜单读写和树查询 | `MenuCommandService` / `MenuQueryService` |
| `/api/Menu` 角色菜单分配 | `RoleMenuAssignmentService` |
| 角色菜单和权限码一起保存 | `RoleAccessAdministration` |
| `MyResult`、布尔投影、自动包装过滤器 | 显式版本接口、`ApiResponse<T>`、`ToHttpResult` |

现有路由、字段和响应行为继续保留。迁移应先提供新契约，再迁移调用方，通过使用情况确认旧接口退出，最后在明确的破坏性版本中删除兼容代码。`ResultMigrationTests`、`MenuQueryBoundaryTests` 和 HTTP 集成测试用于约束过渡期间的行为。

## 8. 数据库迁移与发布

### 8.1 迁移机制

`MigrationRunner` 按顺序执行对应数据库提供程序的嵌入脚本：

| 迁移 | 用途 |
| --- | --- |
| `001_permission_revision` | 权限修订号与活动角色菜单关系唯一索引 |
| `002_authentication` | 用户身份快照与认证会话 |
| `003_stable_permissions` | 稳定权限码表及一次性旧授权导入 |

迁移使用数据库会话锁串行执行，在 `SchemaMigrations` 记录名称、SHA-256 校验和及执行时间。已执行且校验和一致的脚本跳过；修改已记录脚本会导致迁移失败。

新增迁移必须使用有序唯一名称，提供 SQL Server / PostgreSQL 对应实现，并满足事务性和安全重放要求。脚本提交与写入迁移记录不是同一事务：若在两者之间崩溃，下次执行会重放脚本。不要通过手动修改历史校验和绕过失败。

### 8.2 发布流程

1. 备份数据库。首次从旧权限模型升级时，停止绕过修订号协议或仍使用路径权限的旧实例。
2. 检查重复的活动角色菜单关系及未映射的自定义动作授权；迁移会拒绝这些情况，需先按业务含义处理。
3. 发布应用：

   ```powershell
   dotnet publish My.XXX.APIs/01My.XXX.APIs.csproj -c Release -o artifacts/publish
   ```

4. 从发布目录使用目标环境配置执行迁移。迁移身份需要相应 DDL 权限，命令失败时停止部署：

   ```powershell
   Set-Location artifacts/publish
   dotnet My.XXX.APIs.dll --migrate
   if ($LASTEXITCODE -ne 0) { throw '数据库迁移失败，停止发布。' }
   dotnet My.XXX.APIs.dll
   ```

5. 检查 `/ready` 后再接入流量。运行身份需读取 `SchemaMigrations`，无需拥有迁移所用 DDL 权限。

此前手工执行过升级 SQL 的环境，也应运行一次 `--migrate` 建立记录。当前脚本可重复执行。`schema-version` 会校验当前应用所需的全部迁移及其校验和，缺少记录时判为未就绪。

| 就绪检查 | 检查内容 |
| --- | --- |
| `database` | 数据库连接 |
| `schema-version` | 当前应用要求的迁移记录与校验和 |
| `permission-schema` | 权限修订号可查询 |
| `authentication-schema` | 认证会话及稳定权限结构可查询 |
| `redis` | 已配置 Redis 时检查连接 |

这些检查使用 `ready` 标签和五秒超时，不会自动迁移数据库。额外的迁移记录允许存在，便于增量结构变更的滚动升级；这不代表旧应用能兼容任意破坏性结构变更。删除列或改变语义应单独制定兼容与维护计划。

数据库恢复或回退到旧写入程序后，重新检查结构，并更换权限缓存前缀，防止历史修订号复用旧缓存。Data Protection 密钥目录及应用加密密钥应按部署需求持久化；多实例需要协调应用名称、密钥目录和加密密钥。

### 8.3 容器部署

仓库 Dockerfile 使用 .NET 10 Alpine 多阶段构建，运行镜像安装 ICU 和时区数据，以 `app` 用户运行并暴露 `8080` 端口。

```powershell
docker build -t backend-template .

# .env.runtime 由部署系统准备，包含上述配置；不要提交真实凭据。
docker run --rm --env-file .env.runtime backend-template --migrate
if ($LASTEXITCODE -ne 0) { throw '数据库迁移失败，停止发布。' }
docker run --rm -p 8080:8080 --env-file .env.runtime backend-template
```

容器内的数据库地址应能从容器网络访问，不能直接把宿主机数据库当作容器的 `localhost`。按需要挂载 Data Protection 密钥和日志目录，并保证 `app` 用户有写权限。

## 9. 运行日志

`ExceptionHandlingMiddleware` 统一收集请求元数据，不采集请求或响应正文。普通请求尊重日志开关及 `IgnoreMetrics`，异常仍进入错误记录流程。

日志写入 `QueuedRequestLogWriter` 的有界内存队列，请求不等待 SQL 或文本落盘。后台服务为每条记录创建独立 DI scope，并使用写入超时。队列满或关闭时拒绝新记录，增加 `request_logs.dropped` 指标；停止时在宿主关闭期限内尽量排空。

SQL 日志写入失败不会替换原 HTTP 结果；组合存储模式下仍可尝试文本输出。旧邮件模式回退到文本。队列溢出、进程崩溃、写入故障和强制关闭都可能丢失记录，因此此机制不提供可靠业务审计保证。授权变更等需要可靠保存的审计，应单独设计并与业务事务协调。

## 10. 测试与验证

```powershell
dotnet test MyXXXSolution.sln
dotnet publish My.XXX.APIs/01My.XXX.APIs.csproj -c Release
```

测试覆盖分层依赖、功能边界、菜单规则、映射和序列化、响应兼容、缓存修订竞争、会话边界、组合事务及迁移就绪检查。架构测试共用依赖扫描器，展开嵌套泛型、数组、方法体和泛型方法调用，并沿模型属性追踪间接依赖；身份模型不得包含响应 DTO，事务内操作不得依赖事务入口或当前用户。

真实数据库测试需要显式提供可创建和删除数据库的隔离测试服务器连接：

```powershell
$env:ARCH_TEST_POSTGRES = '<PostgreSQL 测试服务器管理连接串>'
$env:ARCH_TEST_SQLSERVER = '<SQL Server 测试服务器管理连接串>'

# CI 可要求两个数据库都必须配置，避免遗漏被误认为验证成功。
$env:ARCH_TEST_REQUIRE_DATABASES = '1'
dotnet test MyXXXSolution.sln
```

每个用例创建并删除自己随机命名的数据库，验证重复迁移、并发授权、排序、异常回滚、组合事务及迁移记录异常。不要指向生产服务器。未配置相应连接时，默认跳过该提供程序的真实数据库用例；跳过不等于已经验证。

## 11. 常见问题

| 现象 | 排查方向 |
| --- | --- |
| 启动提示缺少加密密钥 | 检查进程环境中的 `APP_ENCRYPTION_KEY`，应解码为 32 字节 |
| JWT 配置校验失败 | 检查 Secret 字节长度、Issuer、Audience 和两个有效期 |
| `/healthy` 成功但 `/ready` 失败 | 检查数据库、迁移记录和已配置的 Redis，存活不代表依赖已就绪 |
| 手工建表后 `schema-version` 失败 | 使用当前发布产物执行 `--migrate`，建立并核对迁移记录 |
| JWT 签名正确仍然返回 401 | 检查令牌用途、会话是否存在或被撤销、用户是否启用及令牌是否过期 |
| 勾选菜单后仍返回 403 | 菜单导航与 API 权限独立，检查角色权限码和端点元数据 |
| 访问 Demo 路由返回 404 | Demo 不是活动控制器，使用 Swagger 和健康检查验证服务 |
| 自定义动作导致首次权限迁移失败 | 补齐权限目录与导入映射；已执行的历史迁移不能直接修改 |
| 日志未全部保存 | 检查开关、存储模式、队列丢弃指标、超时和后台写入错误 |
