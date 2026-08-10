using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseApi.Models;

[Table("package_batches")]
public sealed class PackageBatch
{
    [Key, Column("id")]
    public int Id { get; set; }

    [Column("client_id")]
    public int ClientId { get; set; }

    [Required, Column("batch_number")]
    public string BatchNumber { get; set; } = string.Empty;

    [Required, Column("fulfillment_method")]
    public string FulfillmentMethod { get; set; } = string.Empty;

    [Column("delivery_fee", TypeName = "decimal(10,2)")]
    public decimal DeliveryFee { get; set; }

    [Column("delivery_area")]
    public string? DeliveryArea { get; set; }

    [Column("delivery_address")]
    public string? DeliveryAddress { get; set; }

    [Required, Column("delivery_fee_source")]
    public string DeliveryFeeSource { get; set; } = "ClientDefault";

    [Column("delivery_fee_override_reason")]
    public string? DeliveryFeeOverrideReason { get; set; }

    [Required, Column("status")]
    public string Status { get; set; } = "Draft";

    [Column("scheduled_date")]
    public DateTime? ScheduledDate { get; set; }

    [Column("completed_date")]
    public DateTime? CompletedDate { get; set; }

    [Column("paid_date")]
    public DateTime? PaidDate { get; set; }

    [Column("created_by_user_id")]
    public int CreatedByUserId { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Client Client { get; set; } = null!;
    public AppUser CreatedByUser { get; set; } = null!;
    public ICollection<PackageBatchItem> Items { get; set; } = new List<PackageBatchItem>();
}
