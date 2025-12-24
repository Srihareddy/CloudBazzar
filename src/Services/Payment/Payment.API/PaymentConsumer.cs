using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ShopSphere.EventBus;
using ShopSphere.SharedKernel.Events;

namespace Payment.API;

public sealed class PaymentConsumer : IHostedService
{
    private readonly ILogger<PaymentConsumer> _log;
    private readonly IEventBus _bus;
    private readonly RabbitMqConsumerHostedService _consumer;

    public PaymentConsumer(ILogger<PaymentConsumer> log, IEventBus bus, RabbitMqConsumerHostedService consumer)
    {
        _log = log;
        _bus = bus;
        _consumer = consumer;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _consumer.AddSubscription<OrderCreated>(HandleOrderCreatedAsync);
        return Task.CompletedTask;
    }

    private async Task HandleOrderCreatedAsync(OrderCreated evt, CancellationToken ct)
    {
        _log.LogInformation("Payment received OrderCreated for {OrderId} total {Total}", evt.OrderId, evt.Total);

        // Mock payment decision: fail if total > 1000
        if (evt.Total > 1000m)
        {
            await _bus.PublishAsync(new PaymentFailed(evt.OrderId, "Amount exceeded limit", DateTimeOffset.UtcNow), ct);
            return;
        }

        await _bus.PublishAsync(new PaymentSucceeded(evt.OrderId, PaymentId: Guid.NewGuid().ToString("N"), PaidAt: DateTimeOffset.UtcNow), ct);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
