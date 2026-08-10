namespace WarehouseApi.Dtos;

public sealed record TenantDashboardResponse(
    int TenantId,
    string TenantName,
    string? LogoUrl,
    string BatchHandlingMode,
    decimal PerLbRate,
    decimal DefaultDeliveryFee,
    int TotalPackages,
    int ActiveUsers,
    decimal TotalInvoiceValue,
    decimal OutstandingBalance,
    IReadOnlyDictionary<string, int> PackagesByStatus,
    IReadOnlyList<TenantRecentPackageResponse> RecentPackages);

public sealed record TenantRecentPackageResponse(
    int PackageId,
    string PackageNumber,
    string? TrackingId,
    string? CustomerName,
    string? Status,
    decimal? Weight,
    decimal InvoiceAmount,
    decimal AmountDue,
    DateTime? Created,
    string? BatchNumber);
