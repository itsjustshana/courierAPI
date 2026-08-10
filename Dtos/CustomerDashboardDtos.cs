namespace WarehouseApi.Dtos;

public sealed record CustomerDashboardResponse(
    string TenantName, string? LogoUrl, int TotalPackages, decimal TotalInvoiceValue,
    decimal OutstandingBalance, IReadOnlyDictionary<string, int> PackagesByStatus);

public sealed record CustomerProfileResponse(
    string Username, string? Email, string? FirstName, string? LastName,
    string? FullName, string? Mobile, string? HomePhone, string? IdType,
    string? IdNumber, string? PickupLocation, string? Address1,
    string? Address2, string? City, string? Parish);
