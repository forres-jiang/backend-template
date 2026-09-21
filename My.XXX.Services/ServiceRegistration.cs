using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using My.XXX.Contracts.DTOs;
using My.XXX.Services.Authentication;
using My.XXX.Services.Authentication.Interfaces;
using My.XXX.Services.Authorization;
using My.XXX.Services.Authorization.Interfaces;
using My.XXX.Services.Examples;
using My.XXX.Services.Examples.Interfaces;
using My.XXX.Services.Examples.Validators;
using My.XXX.Services.Menus;
using My.XXX.Services.Menus.Interfaces;
using My.XXX.Services.Menus.Mapping;
using My.XXX.Services.Operations;
using My.XXX.Services.Operations.Interfaces;

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
        services.AddScoped<RoleMenuMutations>();
        services.TryAddSingleton(System.TimeProvider.System);
        services.AddScoped<MenuQueryService>();
        services.AddScoped<RoleMenuAssignmentService>();
        services.AddScoped<IPermissionAdministration, PermissionAdministration>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.TryAddScoped<IPermissionQuery, PermissionQuery>();
        return services;
    }
}
