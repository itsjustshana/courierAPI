namespace WarehouseApi.Models;

public class PriceHistory
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    
    // The price at that specific point in time
    public decimal PricePerKg { get; set; }

    public string? UpdatedBy {get; set;}
    
    // The "Effective Date" - Defaults to the moment it's created
    public DateTime EffectiveDate { get; set; } = DateTime.Now;

    public Product Product { get; set; } = null!;
}