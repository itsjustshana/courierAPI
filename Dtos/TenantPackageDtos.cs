namespace WarehouseApi.Dtos;

public sealed record TenantPackageResponse(
    int PackageId, string PackageNumber, string? TrackingId, string? CustomerName,
    string? Description, string? Status, decimal? Weight, decimal InvoiceAmount,
    decimal CustomsDuties, decimal AmountDue, DateTime? PaidDate, DateTime? Created, string? BatchNumber);
