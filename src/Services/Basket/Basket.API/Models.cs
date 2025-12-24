namespace Basket.API.Models;

public sealed class BasketItem
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}

public sealed class CustomerBasket
{
    public string UserId { get; set; } = string.Empty;
    public List<BasketItem> Items { get; set; } = new();
}
