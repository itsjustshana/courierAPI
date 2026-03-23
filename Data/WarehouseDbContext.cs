using Microsoft.EntityFrameworkCore;
using WarehouseApi.Models;

namespace WarehouseApi.Data;

public class WarehouseDbContext : DbContext
{
    public WarehouseDbContext(DbContextOptions<WarehouseDbContext> options) : base(options) { }

    // Your tables
    public DbSet<Product> Products { get; set; } = null!;
    public DbSet<Category> Categories { get; set; } = null!;
    public DbSet<PriceHistory> PriceHistories { get; set; } = null!;
    public DbSet<ProductCategory> ProductCategories { get; set; } = null!;

  
}
