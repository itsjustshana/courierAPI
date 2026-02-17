using System;

namespace WarehouseApi.Dtos;

public class AddProductDto
{
    public string Name { get; set; } = string.Empty;
    public decimal CurrentPricePerKg { get; set; }
    public int CategoryId { get; set; }
    public bool IsAvailable { get; set; } = true;
    public string? Image { get; set; }
}