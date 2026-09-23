using LinqToDB.Extensions.DependencyInjection;
using LinqToDB.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using My.XXX.Persistences.Repositories;
using My.XXX.Services.AccessControl.Ports;
using My.XXX.Services.Authentication.Ports;
using My.XXX.Services.Authorization.Ports;
using My.XXX.Services.Examples.Ports;
using My.XXX.Services.Menus.Ports;
using My.XXX.Services.Operations.Ports;

namespace My.XXX.Persistences;

public static class PersistenceRegistration
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.TryAddScoped<IAuthenticationStore, AuthenticationStore>();
        services.AddScoped<IRolePermissionStore, RolePermissionStore>();
        services.AddScoped<IMenuReadRepository, MenuReadRepository>();
        services.AddScoped<IPermissionStore, PermissionStore>();
        services.AddScoped<IAccessControlTransaction, AccessControlTransaction>();
        services.AddScoped<IDemoRepository, DemoRepository>();
        services.AddScoped<IOperationRepository, OperationRepository>();
        return services;
    }
    public static IServiceCollection AddPersistenceDatabases(this IServiceCollection services, string businessConnection,
        DatabaseProvider businessProvider)
    {
        services.AddLinqToDBContext<DBContext>((provider, options) =>
            DatabaseConfiguration.Configure(options, businessConnection, businessProvider).UseDefaultLogging(provider));
        return services;
    }
}
