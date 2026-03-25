using System.Text.Json.Serialization;

public class PriceUpdateDto
{
    public decimal NewPrice { get; set; }


[JsonPropertyName("effectiveDate")] // This is the "bridge"
    public DateTime EffectiveDate { get; set; }
    public string? UpdatedBy { get; set; }
}