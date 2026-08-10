using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseApi.Models;

[Table("supplier_collection_items")]
public sealed class SupplierCollectionItem
{
    [Column("collection_id")] public int CollectionId { get; set; }
    [Column("package_id")] public int PackageId { get; set; }
    [Column("added_at")] public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public SupplierCollection Collection { get; set; } = null!;
    public UserPackage Package { get; set; } = null!;
}
