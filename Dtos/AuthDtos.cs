using System.ComponentModel.DataAnnotations;

namespace WarehouseApi.Dtos;

public sealed record LoginRequest(
    [Required, MaxLength(150)] string UsernameOrEmail,
    [Required, MaxLength(200)] string Password);

public sealed record LoginResponse(
    string AccessToken,
    DateTime ExpiresAt,
    AuthenticatedUser User);

public sealed record AuthenticatedUser(
    int Id,
    int? TenantId,
    string Username,
    string? FirstName,
    string? LastName,
    string? Email,
    string Role);

public sealed record CreateUserRequest(
    int? TenantId,
    [Required, MaxLength(100)] string Username,
    [Required, MinLength(10), MaxLength(200)] string Password,
    [Required, MaxLength(50)] string Role,
    [MaxLength(100)] string? FirstName,
    [MaxLength(100)] string? LastName,
    [EmailAddress, MaxLength(150)] string? Email);

public sealed record RegisterTenantRequest(
    [Required, MaxLength(200)] string CompanyName,
    [MaxLength(200)] string? ContactName,
    [EmailAddress, MaxLength(150)] string? CompanyEmail,
    [MaxLength(50)] string? Phone,
    [Required, MaxLength(100)] string Username,
    [Required, MinLength(10), MaxLength(200)] string Password,
    [Required, MaxLength(100)] string FirstName,
    [Required, MaxLength(100)] string LastName,
    [Required, EmailAddress, MaxLength(150)] string OwnerEmail);

public sealed record RegisterTenantResponse(
    int TenantId,
    string CompanyName,
    LoginResponse Authentication);

public sealed record SetUserActiveRequest(bool IsActive);

public sealed record ResetPasswordRequest(
    [Required, MinLength(10), MaxLength(200)] string NewPassword);

public sealed record ChangePasswordRequest(
    [Required, MaxLength(200)] string CurrentPassword,
    [Required, MinLength(10), MaxLength(200)] string NewPassword);
