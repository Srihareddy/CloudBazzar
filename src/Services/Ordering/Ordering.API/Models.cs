namespace Ordering.API.Models;

public sealed class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string Status { get; set; } = "Created";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
