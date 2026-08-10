namespace WarehouseApi.Dtos;

public sealed record CustomerDashboardResponse(
    string TenantName, string? LogoUrl, int TotalPackages, decimal TotalInvoiceValue,
    decimal OutstandingBalance, IReadOnlyDictionary<string, int> PackagesByStatus);
