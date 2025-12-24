using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ShopSphere.EventBus;

/// <summary>
/// Minimal consumer that binds a durable queue to the shared exchange using routing keys = CLR type names.
/// Register via AddSubscription<T>(...) below.
/// </summary>
public sealed class RabbitMqConsumerHostedService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly string _queueName;
    private readonly string _hostName;
    private readonly string _userName;
    private readonly string _password;

    private readonly Dictionary<string, Func<string, CancellationToken, Task>> _handlers = new();

    private IConnection? _connection;
    private IModel? _channel;

    private const string ExchangeName = "shopsphere.events";

    public RabbitMqConsumerHostedService(IServiceProvider sp, string queueName, string hostName, string userName, string password)
    {
        _sp = sp;
        _queueName = queueName;
        _hostName = hostName;
        _userName = userName;
        _password = password;
    }

    public void AddSubscription<T>(Func<T, CancellationToken, Task> handler) where T : class
    {
        var key = typeof(T).Name;
        _handlers[key] = async (json, ct) =>
        {
            var obj = JsonSerializer.Deserialize<T>(json);
            if (obj is null) return;
            await handler(obj, ct);
        };
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _hostName,
            UserName = _userName,
            Password = _password,
            DispatchConsumersAsync = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(exchange: ExchangeName, type: ExchangeType.Topic, durable: true);

        _channel.QueueDeclare(queue: _queueName, durable: true, exclusive: false, autoDelete: false);
        foreach (var routingKey in _handlers.Keys)
        {
            _channel.QueueBind(queue: _queueName, exchange: ExchangeName, routingKey: routingKey);
        }

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) =>
        {
            try
            {
                var routingKey = ea.RoutingKey;
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());

                if (_handlers.TryGetValue(routingKey, out var handler))
                {
                    await handler(json, stoppingToken);
                }

                _channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch
            {
                // Basic, safe default: requeue once
                _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);
        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        try { _channel?.Close(); } catch { }
        try { _connection?.Close(); } catch { }
        base.Dispose();
    }
}
