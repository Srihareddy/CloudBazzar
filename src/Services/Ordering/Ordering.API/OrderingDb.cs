using Microsoft.EntityFrameworkCore;
using Ordering.API.Models;

namespace Ordering.API;

public sealed class OrderingDb : DbContext
{
    public OrderingDb(DbContextOptions<OrderingDb> options) : base(options) {}

    public DbSet<Order> Orders => Set<Order>();
}
