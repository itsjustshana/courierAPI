using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseApi.Models;

[Table("users")]
public class AppUser
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    
    [Column("client_id")]
    public int? ClientId { get; set; }

    [Required]
    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [Column("first_name")]
    public string? FirstName { get; set; }

    [Column("last_name")]
    public string? LastName { get; set; }

    [Column("full_name")]
    public string? FullName { get; set; }

    [Column("email")]
    public string? Email { get; set; }

    [Column("mobile")]
    public string? Mobile { get; set; }

    [Column("home_phone")]
    public string? HomePhone { get; set; }

    [Column("id_type")]
    public string? IdType { get; set; }

    [Column("id_number")]
    public string? IdNumber { get; set; }

    [Column("pickup_location")]
    public string? PickupLocation { get; set; }

    [Column("address_1")]
    public string? Address1 { get; set; }

    [Column("address_2")]
    public string? Address2 { get; set; }

    [Column("city")]
    public string? City { get; set; }

    [Column("parish")]
    public string? Parish { get; set; }

    [Column("normalized_email")]
    public string? NormalizedEmail { get; set; }

    [Required]
    [Column("role")]
    public string Role { get; set; } = "User";

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("last_login_at")]
    public DateTime? LastLoginAt { get; set; }

    [Column("failed_login_attempts")]
    public int FailedLoginAttempts { get; set; }

    [Column("locked_until")]
    public DateTime? LockedUntil { get; set; }

    [ForeignKey(nameof(ClientId))]
    public Client? Client { get; set; }
}
