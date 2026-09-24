using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Scalar.AspNetCore;
using Store.ServiceDefaults.Endpoints;
using Store.ServiceDefaults.Http;

namespace Store.ServiceDefaults;

/// <summary>HTTP API conventions: Problem Details, OpenAPI, JSON options and endpoint discovery.</summary>
public static class WebDefaultsExtensions
{
    public static IHostApplicationBuilder AddWebDefaults(this IHostApplicationBuilder builder, Assembly endpointsAssembly)
    {
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddOpenApi();
        builder.Services.Configure<JsonOptions>(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        builder.Services.AddEndpoints(endpointsAssembly);
        return builder;
    }

    public static WebApplication UseWebDefaults(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.MapOpenApi();
        app.MapScalarApiReference();
        app.MapDefaultEndpoints();
        app.MapEndpoints();
        return app;
    }
}
