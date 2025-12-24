using Basket.API;
using Basket.API.Models;
using Serilog;
using ShopSphere.Observability;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) =>
{
    var seq = ctx.Configuration["SEQ_URL"] ?? "http://seq:80";
    lc.Enrich.FromLogContext()
      .WriteTo.Console()
      .WriteTo.Seq(seq);
});

builder.Services.AddShopSphereOpenTelemetry("Basket.API");
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var redis = builder.Configuration.GetConnectionString("Redis") ?? "redis:6379";
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redis));
builder.Services.AddSingleton<BasketStore>();

builder.Services.AddHealthChecks().AddRedis(redis, name: "redis");

var app = builder.Build();

app.MapHealthChecks("/health");

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/basket/{userId}", async (string userId, BasketStore store) =>
{
    var b = await store.GetAsync(userId);
    return b is null ? Results.NotFound() : Results.Ok(b);
});

app.MapPost("/basket", async (CustomerBasket basket, BasketStore store) =>
{
    if (string.IsNullOrWhiteSpace(basket.UserId))
        return Results.BadRequest("UserId required.");

    var updated = await store.UpsertAsync(basket);
    return Results.Ok(updated);
});

app.MapDelete("/basket/{userId}", async (string userId, BasketStore store) =>
{
    await store.DeleteAsync(userId);
    return Results.NoContent();
});

app.Run();
