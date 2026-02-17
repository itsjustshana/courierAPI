using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace WarehouseApi.Models;

[Table("Products")]
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal CurrentPricePerKg { get; set; }
    public bool IsAvailable { get; set; } = true;

    public string? Image { get; set; }
    
    // Tracks the last time the record was touched
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public int CategoryId { get; set; }

    // [JsonIgnore]
    // public Category? Category { get; set; }

    // Navigation property for the Effective Date tracking
    // [JsonIgnore]
    // public List<PriceHistory> PriceHistories { get; set; } = new();
}