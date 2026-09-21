using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using My.XXX.Contracts.DTOs;
using My.XXX.Services.Interfaces;
using My.XXX.Services.Mapping;
using My.XXX.Services.Validators;

namespace My.XXX.Services;

public static class ServiceRegistration
{
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        services.AddSingleton<ApplicationMapper>();
        services.AddScoped<IValidator<DemoModel>, DemoValidator>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IDemoService, DemoService>();
        services.AddScoped<IOperationService, OperationService>();
        services.AddScoped<IMenuService, MenuService>();
        services.AddScoped<MenuCommandService>();
        services.AddScoped<MenuMutations>();
        services.TryAddSingleton(System.TimeProvider.System);
        services.AddScoped<MenuQueryService>();
        services.AddScoped<RolePermissionService>();
        services.AddScoped<IPermissionAdministration, PermissionAdministration>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.TryAddScoped<IPermissionQuery, PermissionQuery>();
        return services;
    }
}
