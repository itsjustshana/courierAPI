namespace WarehouseApi.Dtos;

public sealed record AdminOverviewResponse(
    int Tenants,
    int ActiveTenants,
    int Users,
    int ActiveUsers,
    int AssignedPackages,
    int UnassignedPackages,
    IReadOnlyDictionary<string, int> UsersByRole);

public sealed record TenantAdminResponse(
    int Id,
    string CompanyName,
    string? ContactName,
    string? Email,
    string? Phone,
    string? Address1,
    string? Address2,
    string? City,
    string? Zip,
    string? State,
    string? LogoUrl,
    decimal PerLbCost,
    decimal PerLbMarkup,
    decimal PerLbRate,
    string BatchHandlingMode,
    decimal DefaultDeliveryFee,
    bool IsActive,
    DateTime CreatedAt,
    int UserCount);

public sealed record UpdateTenantRatesRequest(decimal PerLbCost, decimal PerLbMarkup);
public sealed record UpdateTenantBatchModeRequest(string BatchHandlingMode);
public sealed record UpdateTenantDeliveryFeeRequest(decimal DefaultDeliveryFee);
public sealed record UpdateTenantRequest(
    string CompanyName,
    string? ContactName,
    string? Email,
    string? Phone,
    string? Address1,
    string? Address2,
    string? City,
    string? Zip,
    string? State,
    string? LogoUrl,
    decimal PerLbCost,
    decimal PerLbMarkup,
    string BatchHandlingMode,
    decimal DefaultDeliveryFee,
    bool IsActive);

public sealed record UserDashboardPreviewResponse(int UserId, string DisplayName,
    string Username, string Role, string? TenantName, bool IsActive,
    int TotalPackages, decimal TotalInvoiceValue, decimal OutstandingBalance,
    int TotalCollections, int OpenCollections,
    IReadOnlyDictionary<string, int> PackagesByStatus,
    IReadOnlyList<UserDashboardPreviewPackage> RecentPackages);
public sealed record UserDashboardPreviewPackage(int PackageId, string PackageNumber,
    string? TrackingId, string? CustomerName, string? Status, decimal? Weight,
    decimal? InvoiceAmount, decimal? AmountDue, DateTime? PaidDate, DateTime? Created);
