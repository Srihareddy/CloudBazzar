using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using ShopSphere.EventBus;
using ShopSphere.Observability;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddShopSphereOpenTelemetry("Notification.Worker");

builder.Services.AddSingleton(_ =>
    new RabbitMqConsumerHostedService(
        builder.Services.BuildServiceProvider(),
        queueName: "notification.queue",
        hostName: builder.Configuration["RABBITMQ_HOST"] ?? "rabbitmq",
        userName: builder.Configuration["RABBITMQ_USER"] ?? "guest",
        password: builder.Configuration["RABBITMQ_PASS"] ?? "guest"
    ));

builder.Services.AddHostedService(sp => sp.GetRequiredService<RabbitMqConsumerHostedService>());
builder.Services.AddHostedService<Notification.Worker.Worker>();

builder.Services.AddSerilog((sp, lc) =>
{
    var seq = builder.Configuration["SEQ_URL"] ?? "http://seq:80";
    lc.WriteTo.Console()
      .WriteTo.Seq(seq);
});

var host = builder.Build();
host.Run();
