namespace WarehouseApi.Dtos;

public sealed record AdminUserResponse(
    int Id, int? TenantId, string? TenantName, string Username,
    string? FirstName, string? LastName, string? Email, string Role,
    bool IsActive, DateTime CreatedAt, DateTime? LastLoginAt);

public sealed record UpdateAdminUserRequest(
    int? TenantId, string Username, string? FirstName, string? LastName,
    string? Email, string Role, bool IsActive);
