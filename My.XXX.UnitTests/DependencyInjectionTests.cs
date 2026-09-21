using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using My.XXX.APIs.Common.DI;
using My.XXX.Infrastructure;
using My.XXX.Contracts.DTOs;

namespace My.XXX.UnitTests;

[TestClass]
public class DependencyInjectionTests
{
    [TestMethod]
    public void ScannedAndExplicitServicesAreSharedOnlyWithinTheirScope()
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddApplicationServices();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();

        var http = first.ServiceProvider.GetRequiredService<IHttpService>();
        Assert.IsInstanceOfType<HttpService>(http);
        Assert.AreSame(http, first.ServiceProvider.GetRequiredService<IHttpService>());
        Assert.AreNotSame(http, second.ServiceProvider.GetRequiredService<IHttpService>());

        var validator = first.ServiceProvider.GetRequiredService<IValidator<DemoModel>>();
        Assert.AreSame(validator, first.ServiceProvider.GetRequiredService<IValidator<DemoModel>>());
        Assert.AreNotSame(validator, second.ServiceProvider.GetRequiredService<IValidator<DemoModel>>());
    }
}
