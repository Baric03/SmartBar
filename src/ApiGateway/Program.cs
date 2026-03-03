using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// --- OpenTelemetry Configuration ---
var serviceName = "ApiGateway";

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(serviceName))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter());

builder.Services.AddHealthChecks();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add YARP reverse proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/api-docs/order", "Order Service");
    c.SwaggerEndpoint("/api-docs/inventory", "Inventory Service");
    c.SwaggerEndpoint("/api-docs/bar", "Bar Service");
    c.SwaggerEndpoint("/api-docs/notification", "Notification Service");
});

app.MapHealthChecks("/health");
app.MapReverseProxy();

app.Run();
