using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using My.XXX.Service.DTOs;
using My.XXX.Service.Interfaces;
using My.XXX.Service.Mapping;
using My.XXX.Service.Validators;
namespace My.XXX.Service;
public static class ServiceRegistration
{
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        services.AddSingleton<ApplicationMapper>();
        services.AddScoped<IValidator<DemoModel>, DemoValidator>();
        services.AddScoped<IValidator<Mail>, MailValidator>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IDemoService, DemoService>();
        services.AddScoped<IMailService, MailService>();
        services.AddScoped<IOperationService, OperationService>();
        services.AddScoped<IMenuService, MenuService>();
        services.AddScoped<MenuCommandService>();
        services.AddScoped<MenuQueryService>();
        services.AddScoped<RolePermissionService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IPermissionQuery, PermissionQuery>();
        services.AddOptions<Common.PermissionCacheOptions>();
        return services;
    }
}
