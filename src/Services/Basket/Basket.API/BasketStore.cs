using System.Text.Json;
using Basket.API.Models;
using StackExchange.Redis;

namespace Basket.API;

public sealed class BasketStore
{
    private readonly IDatabase _db;

    public BasketStore(IConnectionMultiplexer mux)
    {
        _db = mux.GetDatabase();
    }

    public async Task<CustomerBasket?> GetAsync(string userId)
    {
        var v = await _db.StringGetAsync(Key(userId));
        if (v.IsNullOrEmpty) return null;
        return JsonSerializer.Deserialize<CustomerBasket>(v!);
    }

    public async Task<CustomerBasket> UpsertAsync(CustomerBasket basket)
    {
        var json = JsonSerializer.Serialize(basket);
        await _db.StringSetAsync(Key(basket.UserId), json);
        return basket;
    }

    public async Task DeleteAsync(string userId)
    {
        await _db.KeyDeleteAsync(Key(userId));
    }

    private static string Key(string userId) => $"basket:{userId}";
}
