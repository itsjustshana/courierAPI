using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseApi.Models;

[Table("package_batch_items")]
public sealed class PackageBatchItem
{
    [Column("batch_id")]
    public int BatchId { get; set; }

    [Column("package_id")]
    public int PackageId { get; set; }

    [Column("added_at")]
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    public PackageBatch Batch { get; set; } = null!;
    public UserPackage Package { get; set; } = null!;
}
