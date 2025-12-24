using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Ordering.API;
using Ordering.API.Models;
using Serilog;
using ShopSphere.EventBus;
using ShopSphere.SharedKernel.Events;
using ShopSphere.Observability;
using ShopSphere.SharedKernel;


var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) =>
{
    var seq = ctx.Configuration["SEQ_URL"] ?? "http://seq:80";
    lc.Enrich.FromLogContext()
      .WriteTo.Console()
      .WriteTo.Seq(seq);
});

builder.Services.AddShopSphereOpenTelemetry("Ordering.API");
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Ordering.API",
        Version = "v1"
    });


    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter token like: Bearer {your token}"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddDevJwtAuth(builder.Configuration);


var cs = builder.Configuration.GetConnectionString("OrderingDb")
         ?? "Server=sqlserver;Database=ordering;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True;";

builder.Services.AddDbContext<OrderingDb>(o => o.UseSqlServer(cs));
builder.Services.AddHealthChecks().AddSqlServer(cs, name: "sqlserver");

builder.Services.AddSingleton<IEventBus>(_ =>
    new RabbitMqEventBus(
        hostName: builder.Configuration["RABBITMQ_HOST"] ?? "rabbitmq",
        userName: builder.Configuration["RABBITMQ_USER"] ?? "guest",
        password: builder.Configuration["RABBITMQ_PASS"] ?? "guest"
    ));

var app = builder.Build();
app.UsePathBase("/ordering");


app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/ordering/swagger/v1/swagger.json", "Ordering.API v1");
    c.RoutePrefix = "swagger"; // so UI is /ordering/swagger
});

app.MapHealthChecks("/health");

app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderingDb>();
    db.Database.EnsureCreated();
}

app.MapGet("/orders", [Authorize] async (OrderingDb db, HttpContext ctx) =>
{
    var userId = ctx.User.FindFirst("sub")?.Value ?? ctx.User.Identity?.Name ?? "unknown";
    return await db.Orders
        .Where(o => o.UserId == userId)
        .OrderByDescending(o => o.CreatedAt)
        .ToListAsync();
});

app.MapPost("/orders", [Authorize] async (CreateOrderRequest req, OrderingDb db, IEventBus bus, HttpContext ctx) =>
{
    var userId = ctx.User.FindFirst("sub")?.Value ?? "unknown";

    var order = new Order
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Total = req.Total,
        Status = "Created",
        CreatedAt = DateTimeOffset.UtcNow
    };

    db.Orders.Add(order);
    await db.SaveChangesAsync();

    await bus.PublishAsync(new OrderCreated(order.Id, userId, order.Total, order.CreatedAt));
    return Results.Accepted($"/orders/{order.Id}", order);
});

app.MapGet("/orders/{id:guid}", [Authorize] async (Guid id, OrderingDb db, HttpContext ctx) =>
{
    var userId = ctx.User.FindFirst("sub")?.Value ?? "unknown";
    var order = await db.Orders.FindAsync(id);
    if (order is null || order.UserId != userId) return Results.NotFound();
    return Results.Ok(order);
});

app.Run();

public sealed record CreateOrderRequest(decimal Total);
