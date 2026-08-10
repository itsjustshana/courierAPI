using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseApi.Models;

[Table("global_settings")]
public sealed class GlobalSetting
{
    [Key, Column("id")]
    public int Id { get; set; }

    [Required, Column("app_name")]
    public string AppName { get; set; } = "MekMiCourier";

    [Required, Column("supplier")]
    public string Supplier { get; set; } = "Supplier";

    [Column("logo_url", TypeName = "text")]
    public string? LogoUrl { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
