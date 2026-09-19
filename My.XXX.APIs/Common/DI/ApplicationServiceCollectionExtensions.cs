using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using My.XXX.APIs.Common.JWT;
using My.XXX.Infrastructure;
using My.XXX.Service;
using My.XXX.Service.DTOs;
using My.XXX.Service.Interfaces;
using My.XXX.Service.Validators;
using My.XXX.Shared;

namespace My.XXX.APIs.Common.DI;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IWebHelper, WebHelper>();
        services.AddScoped<IValidator<DemoModel>, DemoValidator>();
        services.AddScoped<IValidator<Mail>, MailValidator>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICurrentRequest, HttpCurrentRequest>();
        services.AddScoped<ITokenIssuer, JwtTokenIssuer>();

        // Service、Repository 和 Infrastructure 按标记接口自动注册。
        services.Scan(scan => scan
            .FromAssemblies(typeof(My.XXX.Persistence.DBContext).Assembly,
                typeof(UserService).Assembly, typeof(HttpService).Assembly)
            .AddClasses(classes => classes.AssignableTo<IScopeDependency>(), publicOnly: false)
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        return services;
    }
}
