using Catalog.API;
using Catalog.API.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ShopSphere.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) =>
{
    var seq = ctx.Configuration["SEQ_URL"] ?? "http://seq:80";
    lc.ReadFrom.Configuration(ctx.Configuration)
      .Enrich.FromLogContext()
      .WriteTo.Console()
      .WriteTo.Seq(seq);
});

builder.Services.AddShopSphereOpenTelemetry("Catalog.API");

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var cs = builder.Configuration.GetConnectionString("CatalogDb")
         ?? "Host=postgres;Port=5432;Database=catalog;Username=postgres;Password=postgres";

builder.Services.AddDbContext<CatalogDb>(o => o.UseNpgsql(cs));

builder.Services.AddHealthChecks()
    .AddNpgSql(cs, name: "postgres");

var app = builder.Build();

app.MapHealthChecks("/health");

app.UseSwagger();
app.UseSwaggerUI();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CatalogDb>();
    db.Database.EnsureCreated();

    if (!db.Products.Any())
    {
        db.Products.AddRange(
            new Product { Sku = "SKU-100", Name = "Cloud Hoodie", Description = "Soft hoodie", Price = 49.99m, Stock = 25 },
            new Product { Sku = "SKU-200", Name = "Microservices Mug", Description = "Coffee mug", Price = 14.99m, Stock = 100 }
        );
        db.SaveChanges();
    }
}

app.MapGet("/products", async (CatalogDb db) =>
    await db.Products.OrderByDescending(p => p.CreatedAt).ToListAsync());

app.MapGet("/products/{id:guid}", async (Guid id, CatalogDb db) =>
{
    var p = await db.Products.FindAsync(id);
    return p is null ? Results.NotFound() : Results.Ok(p);
});

app.MapPost("/products", async (Product input, CatalogDb db) =>
{
    input.Id = Guid.NewGuid();
    input.CreatedAt = DateTimeOffset.UtcNow;
    db.Products.Add(input);
    await db.SaveChangesAsync();
    return Results.Created($"/products/{input.Id}", input);
});

app.MapPut("/products/{id:guid}", async (Guid id, Product input, CatalogDb db) =>
{
    var existing = await db.Products.FindAsync(id);
    if (existing is null) return Results.NotFound();

    existing.Sku = input.Sku;
    existing.Name = input.Name;
    existing.Description = input.Description;
    existing.Price = input.Price;
    existing.Stock = input.Stock;

    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapDelete("/products/{id:guid}", async (Guid id, CatalogDb db) =>
{
    var existing = await db.Products.FindAsync(id);
    if (existing is null) return Results.NotFound();
    db.Products.Remove(existing);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();
