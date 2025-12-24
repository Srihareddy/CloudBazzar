using Payment.API;
using Serilog;
using ShopSphere.EventBus;
using ShopSphere.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) =>
{
    var seq = ctx.Configuration["SEQ_URL"] ?? "http://seq:80";
    lc.Enrich.FromLogContext()
      .WriteTo.Console()
      .WriteTo.Seq(seq);
});

builder.Services.AddShopSphereOpenTelemetry("Payment.API");
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Payment.API",   // ✅ fixed
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter JWT token like: Bearer {your token}"
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

builder.Services.AddSingleton<IEventBus>(_ =>
    new RabbitMqEventBus(
        hostName: builder.Configuration["RABBITMQ_HOST"] ?? "rabbitmq",
        userName: builder.Configuration["RABBITMQ_USER"] ?? "guest",
        password: builder.Configuration["RABBITMQ_PASS"] ?? "guest"
    ));

builder.Services.AddSingleton(_ =>
    new RabbitMqConsumerHostedService(
        builder.Services.BuildServiceProvider(),
        queueName: "payment.queue",
        hostName: builder.Configuration["RABBITMQ_HOST"] ?? "rabbitmq",
        userName: builder.Configuration["RABBITMQ_USER"] ?? "guest",
        password: builder.Configuration["RABBITMQ_PASS"] ?? "guest"
    ));

builder.Services.AddHostedService(sp => sp.GetRequiredService<RabbitMqConsumerHostedService>());
builder.Services.AddHostedService<PaymentConsumer>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Payment.API v1");
});

app.MapGet("/", () => Results.Ok(new { service = "Payment.API", status = "running" }));

app.Run();
