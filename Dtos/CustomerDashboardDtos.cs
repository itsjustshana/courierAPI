namespace WarehouseApi.Dtos;

public sealed record CustomerDashboardResponse(
    string TenantName, string? LogoUrl, string? FullName, string? Address1, string? Address2,
    string? City, string? Zip, string? State, int TotalPackages,
    decimal OutstandingBalance, IReadOnlyDictionary<string, int> PackagesByStatus);

public sealed record CustomerProfileResponse(
    string Username, string? Email, string? FirstName, string? LastName,
    string? FullName, string? Mobile, string? HomePhone, string? IdType,
    string? IdNumber, string? PickupLocation, string? Address1,
    string? Address2, string? City, string? Parish);
