using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace WarehouseApi.Models;

public class ProductImage
{
    public int Id { get; set; }

    public int ClientId { get; set; }

    public string? Name { get; set; }

    public string? FileName { get; set; }

    public string ContentType { get; set; } = string.Empty;

    public byte[] ImageData { get; set; } = Array.Empty<byte>();

    public DateTime CreatedAt { get; set; }

    public bool IsActive { get; set; }

    public Client Client { get; set; } = null!;

}