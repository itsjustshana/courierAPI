using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseApi.Models;

[Table("clients")]
public class Client
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("company_name")]
    public string CompanyName { get; set; } = string.Empty;

    [Column("contact_name")]
    public string? ContactName { get; set; }

    [Column("email")]
    public string? Email { get; set; }

    [Column("phone")]
    public string? Phone { get; set; }

    [Column("address1")]
    public string? Address1 { get; set; }

    [Column("address2")]
    public string? Address2 { get; set; }

    [Column("city")]
    public string? City { get; set; }

    [Column("zip")]
    public string? Zip { get; set; }

    [Column("state")]
    public string? State { get; set; }

    [Column("logo_url")]
    public string? LogoUrl { get; set; }

    [Column("per_lb_cost", TypeName = "decimal(10,2)")]
    public decimal PerLbCost { get; set; }

    [Column("per_lb_markup", TypeName = "decimal(10,2)")]
    public decimal PerLbMarkup { get; set; }

    [Required]
    [Column("batch_handling_mode")]
    public string BatchHandlingMode { get; set; } = "None";

    [Column("default_delivery_fee", TypeName = "decimal(10,2)")]
    public decimal DefaultDeliveryFee { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
    public ICollection<PackageBatch> PackageBatches { get; set; } = new List<PackageBatch>();

    [NotMapped]
    public decimal PerLbRate => PerLbCost;
}
