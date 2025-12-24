using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ShopSphere.EventBus;
using ShopSphere.SharedKernel.Events;

namespace Notification.Worker;

public sealed class Worker : IHostedService
{
    private readonly ILogger<Worker> _log;
    private readonly RabbitMqConsumerHostedService _consumer;

    public Worker(ILogger<Worker> log, RabbitMqConsumerHostedService consumer)
    {
        _log = log;
        _consumer = consumer;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _consumer.AddSubscription<PaymentSucceeded>(async (evt, ct) =>
        {
            _log.LogInformation("NOTIFY: PaymentSucceeded for Order {OrderId} Payment {PaymentId}", evt.OrderId, evt.PaymentId);
            await Task.CompletedTask;
        });

        _consumer.AddSubscription<PaymentFailed>(async (evt, ct) =>
        {
            _log.LogWarning("NOTIFY: PaymentFailed for Order {OrderId} reason={Reason}", evt.OrderId, evt.Reason);
            await Task.CompletedTask;
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
