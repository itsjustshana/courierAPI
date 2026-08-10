using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseApi.Models;

[Table("user_package_assignments")]
public sealed class UserPackageAssignment
{
    [Key, Column("package_id")]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int PackageId { get; set; }

    [Column("client_id")]
    public int ClientId { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("assigned_by_user_id")]
    public int AssignedByUserId { get; set; }

    [Column("assigned_at")]
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("per_lb_cost", TypeName = "decimal(10,2)")]
    public decimal PerLbCost { get; set; }

    [Column("per_lb_markup", TypeName = "decimal(10,2)")]
    public decimal PerLbMarkup { get; set; }

    [Column("invoice_cost", TypeName = "decimal(12,2)")]
    public decimal InvoiceCost { get; set; }

    public UserPackage Package { get; set; } = null!;
    public Client Client { get; set; } = null!;
    public AppUser User { get; set; } = null!;
    public AppUser AssignedByUser { get; set; } = null!;
}
