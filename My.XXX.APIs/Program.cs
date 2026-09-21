using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using My.XXX.APIs.Common.DI;
using My.XXX.APIs.Common.Middleware;
using My.XXX.APIs.Configurations;
using Serilog;
using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace My.XXX.APIs
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                var app = CreateApplication(args);
                Log.Information("Application Starting.");
                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "The Application failed to start.");
                throw;
            }
            finally { Log.CloseAndFlush(); }
        }

        public static WebApplication CreateApplication(string[] args, Action<WebApplicationBuilder> configure = null)
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                Args = args,
                ApplicationName = typeof(Program).Assembly.FullName
            });
            ConfigureSources(builder, args);
            configure?.Invoke(builder);

            //Serilog
            builder.Host.UseSerilog((context, logger) =>
            {
                logger.ReadFrom.Configuration(context.Configuration);
                logger.Enrich.FromLogContext();
            });

            builder.Services.AddConfiguration(builder.Configuration);
            builder.Services.AddDefaultService(builder.Configuration);
            builder.Services.AddDBs(builder.Configuration);
            builder.Services.AddApplicationServices();

            var app = builder.Build();
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            var supportedCultures = new[]
            {
                new CultureInfo(CultureType.en_US),
                new CultureInfo(CultureType.zh_CN)
            };

            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture(supportedCultures.First()),
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures,
                ApplyCurrentCultureToResponseHeaders = true
            });

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("./v1/swagger.json", "XXX API v1"));
            }

            app.UseMiddleware<ExceptionHandlingMiddleware>();
            app.UseStaticFiles();
            app.UseRouting();

            //跨域
            app.UseCors("AppPolicy");
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.MapHealthChecks("/healthy", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                Predicate = _ => false
            }).AllowAnonymous();
            app.MapHealthChecks("/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("ready")
            }).AllowAnonymous();
            return app;
        }

        public static void ConfigureSources(WebApplicationBuilder builder, string[] args)
        {
            var directory = Path.Combine(builder.Environment.ContentRootPath, "Configurations");
            builder.Configuration
                .AddJsonFile(Path.Combine(directory, "appsettings.json"), optional: false, reloadOnChange: true)
                .AddJsonFile(Path.Combine(directory, $"appsettings.{builder.Environment.EnvironmentName}.json"),
                    optional: false, reloadOnChange: true);
            if (builder.Environment.IsDevelopment())
                builder.Configuration.AddUserSecrets<Program>(optional: true);
            builder.Configuration.AddEnvironmentVariables().AddCommandLine(args);
        }
    }
}
