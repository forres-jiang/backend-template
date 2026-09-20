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
        services.AddScoped<IMenuRepository, MenuRepository>();
        services.AddScoped<IMailRepository, MailRepository>();
        services.AddScoped<IDemoRepository, DemoRepository>();
        services.AddScoped<IOperationRepository, OperationRepository>();
        return services;
    }
    public static IServiceCollection AddPersistenceDatabases(this IServiceCollection services, string businessConnection,
        DatabaseProvider businessProvider, string mailConnection, DatabaseProvider mailProvider)
    {
        services.AddLinqToDBContext<DBContext>((provider, options) =>
            DatabaseConfiguration.Configure(options, businessConnection, businessProvider).UseDefaultLogging(provider));
        services.AddLinqToDBContext<MailContext>((provider, options) =>
            DatabaseConfiguration.Configure(options, mailConnection, mailProvider).UseDefaultLogging(provider));
        return services;
    }
}
