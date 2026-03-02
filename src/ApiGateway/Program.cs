using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

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
