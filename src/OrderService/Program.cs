using Microsoft.EntityFrameworkCore;
using OrderService.Core.Interfaces;
using OrderService.Core.Services;
using OrderService.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddControllers();

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register Services
builder.Services.AddScoped<IOrderService, OrderManagementService>();
builder.Services.AddSingleton<OrderService.Messaging.IKafkaProducer, OrderService.Messaging.KafkaProducer>();

builder.Services.AddGrpcClient<InventoryService.Protos.InventoryGrpcConfig.InventoryGrpcConfigClient>(o =>
{
    o.Address = new Uri(builder.Configuration["GrpcUrls:InventoryService"] ?? "https://localhost:7042");
}).ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler();
    handler.ServerCertificateCustomValidationCallback = 
        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    return handler;
});

var app = builder.Build();

// Migrate and Seed Database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    DbSeeder.Seed(context);
}

app.MapControllers();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    // Swagger UI is available at: https://localhost:7224/swagger
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Run();
