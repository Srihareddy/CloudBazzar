namespace ShopSphere.EventBus;

public interface IEventBus
{
    Task PublishAsync<T>(T evt, CancellationToken ct = default) where T : class;
}
