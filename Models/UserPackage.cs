using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseApi.Models;

[Table("UserPackages")]
public sealed class UserPackage
{
    [Key, Column("package_id")]
    public int PackageId { get; set; }

    [Column("userid")]
    public int? SourceUserId { get; set; }

    [Column("created")]
    public DateTime? Created { get; set; }

    [Column("full_name")]
    public string? FullName { get; set; }

    [Column("no_profit")]
    public string? NoProfit { get; set; }

    [Column("package_number")]
    public string PackageNumber { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("status")]
    public string? Status { get; set; }

    [Column("tracking_id")]
    public string? TrackingId { get; set; }

    [Column("weight")]
    public decimal? Weight { get; set; }

    [Column("invoice_amount")]
    public decimal? InvoiceAmount { get; set; }

    [Column("amount_due")]
    public decimal? AmountDue { get; set; }

    [Column("handling")]
    public decimal? Handling { get; set; }

    [Column("paid_date")]
    public DateTime? PaidDate { get; set; }

    [Column("system_status")]
    public string? SystemStatus { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("customs_charges")]
    public decimal? CustomsCharges { get; set; }

    [Column("additional_markup")]
    public decimal? AdditionalMarkup { get; set; }

    [Column("supplier_amount", TypeName = "decimal(12,2)")]
    public decimal? SupplierAmount { get; set; }

    [Column("supplier_paid_date")]
    public DateTime? SupplierPaidDate { get; set; }

    [Column("supplier_payment_reference")]
    public string? SupplierPaymentReference { get; set; }

    [Column("invoiceready")]
    public bool? InvoiceReady { get; set; }

    [Column("DropoffDate")]
    public DateTime? DropoffDate { get; set; }

    public UserPackageAssignment? Assignment { get; set; }
    public PackageBatchItem? BatchItem { get; set; }
    public SupplierCollectionItem? SupplierCollectionItem { get; set; }
}
