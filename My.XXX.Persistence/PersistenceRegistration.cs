using LinqToDB.Extensions.DependencyInjection;
using LinqToDB.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using My.XXX.Persistence.Repositories;
using My.XXX.Service.Ports;
namespace My.XXX.Persistence;

public static class PersistenceRegistration
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<MenuRepository>();
        services.AddScoped<IMenuReadRepository>(sp => sp.GetRequiredService<MenuRepository>());
        services.AddScoped<IPermissionStore>(sp => sp.GetRequiredService<MenuRepository>());
        services.AddScoped<IMenuTransaction>(sp => sp.GetRequiredService<MenuRepository>());
        services.AddScoped<IDemoRepository, DemoRepository>();
        services.AddScoped<IOperationRepository, OperationRepository>();
        return services;
    }
    public static IServiceCollection AddPersistenceDatabases(this IServiceCollection services, string businessConnection,
        DatabaseProvider businessProvider, string mailConnection, DatabaseProvider mailProvider)
    {
        services.AddLinqToDBContext<DBContext>((provider, options) =>
            DatabaseConfiguration.Configure(options, businessConnection, businessProvider).UseDefaultLogging(provider));
        return services;
    }
}
