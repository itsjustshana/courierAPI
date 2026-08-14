using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseApi.Models;

[Table("supplier_collections")]
public sealed class SupplierCollection
{
    [Key, Column("id")] public int Id { get; set; }
    [Required, Column("collection_number")] public string CollectionNumber { get; set; } = string.Empty;
    [Required, Column("supplier_name")] public string SupplierName { get; set; } = string.Empty;
    [Column("bearer_user_id")] public int? BearerUserId { get; set; }
    [Required, Column("status")] public string Status { get; set; } = "Open";
    [Column("completed_at")] public DateTime? CompletedAt { get; set; }
    [Column("created_by_user_id")] public int CreatedByUserId { get; set; }
    [Column("notes")] public string? Notes { get; set; }
    [Column("collection_date")] public DateTime CollectionDate { get; set; } = DateTime.UtcNow.Date;
    [Column("paid_date")] public DateTime? PaidDate { get; set; }
    [Column("created_at")] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public AppUser CreatedByUser { get; set; } = null!;
    public AppUser? BearerUser { get; set; }
    public ICollection<SupplierCollectionItem> Items { get; set; } = new List<SupplierCollectionItem>();
}
